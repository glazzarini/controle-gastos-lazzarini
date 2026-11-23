import { useEffect, useState, type MouseEvent } from 'react';
import { api } from '../lib/api';
import { dataParaIso, isoParaBr } from '../lib/datas';
import type { Lancamento } from '../lib/types';
import { useAppData } from '../state/AppData';
import { useUi } from '../state/Ui';
import { useCategoriasOrdenadas } from '../state/hooks';

interface Props {
  lancamento: Lancamento | null;
  onFechar: () => void;
}

export function EditLancamentoModal({ lancamento, onFechar }: Props) {
  const { recarregar } = useAppData();
  const { toast } = useUi();
  const categorias = useCategoriasOrdenadas();
  const [campos, setCampos] = useState({ descricao: '', categoria: '', valor: '', data: '', tipo: 'A vista', observacao: '' });
  const [salvando, setSalvando] = useState(false);

  useEffect(() => {
    if (lancamento) {
      setCampos({
        descricao: lancamento.descricao,
        categoria: lancamento.categoria,
        valor: String(lancamento.valor),
        data: dataParaIso(lancamento.data),
        tipo: lancamento.tipo,
        observacao: lancamento.observacao,
      });
    }
  }, [lancamento]);

  const alterar = (campo: keyof typeof campos) => (e: { target: { value: string } }) =>
    setCampos(c => ({ ...c, [campo]: e.target.value }));

  const aoClicarOverlay = (e: MouseEvent) => { if (e.target === e.currentTarget) onFechar(); };

  const salvar = async () => {
    if (!lancamento) return;
    const valor = parseFloat(campos.valor);
    // Valor 0/vazio é rejeitado; negativo (crédito) é aceito.
    if (!campos.data || !campos.categoria || !valor) {
      toast('Preencha todos os campos', 'error');
      return;
    }
    setSalvando(true);
    try {
      await api.atualizarLancamento(lancamento.id, {
        data: isoParaBr(campos.data),
        descricao: campos.descricao.trim(),
        categoria: campos.categoria,
        valor,
        observacao: campos.observacao.trim(),
        tipo: campos.tipo,
      });
      await recarregar();
      onFechar();
      toast('Lançamento atualizado ✓', 'success');
    } catch (e) {
      toast(`Erro ao salvar: ${(e as Error).message}`, 'error');
    } finally {
      setSalvando(false);
    }
  };

  return (
    <div className={`edit-modal-overlay${lancamento ? ' open' : ''}`} onClick={aoClicarOverlay} data-testid="edit-modal">
      <div className="edit-modal-sheet">
        <div className="edit-modal-title">Editar lançamento</div>
        <div className="form-group">
          <label className="form-label" htmlFor="edit-descricao">Descrição</label>
          <input className="form-input" id="edit-descricao" type="text" value={campos.descricao} onChange={alterar('descricao')} />
        </div>
        <div className="form-group">
          <label className="form-label" htmlFor="edit-categoria">Categoria</label>
          <select className="form-select" id="edit-categoria" value={campos.categoria} onChange={alterar('categoria')}>
            {categorias.map(n => <option key={n}>{n}</option>)}
          </select>
        </div>
        <div className="form-group">
          <label className="form-label" htmlFor="edit-valor">Valor</label>
          <div className="value-input-wrap">
            <span className="value-prefix">R$</span>
            <input className="form-input" id="edit-valor" type="number" inputMode="decimal" step="0.01" value={campos.valor} onChange={alterar('valor')} />
          </div>
        </div>
        <div className="form-row">
          <div className="form-group">
            <label className="form-label" htmlFor="edit-data">Data</label>
            <input className="form-input" id="edit-data" type="date" value={campos.data} onChange={alterar('data')} />
          </div>
          <div className="form-group">
            <label className="form-label" htmlFor="edit-tipo">Tipo</label>
            <select className="form-select" id="edit-tipo" value={campos.tipo} onChange={alterar('tipo')}>
              <option>A vista</option>
              <option>Parcelado</option>
            </select>
          </div>
        </div>
        <div className="form-group">
          <label className="form-label" htmlFor="edit-obs">Observação <span style={{ color: 'var(--muted)', fontWeight: 400 }}>(opcional)</span></label>
          <input className="form-input" id="edit-obs" type="text" placeholder="Ex: Pneus do TCross, Porto de Galinhas..." value={campos.observacao} onChange={alterar('observacao')} />
        </div>
        <button className="edit-save-btn" onClick={salvar} disabled={salvando}>
          {salvando ? 'Salvando...' : 'Salvar alterações'}
        </button>
      </div>
    </div>
  );
}
