import { useState } from 'react';
import { api } from '../lib/api';
import { adicionarMeses, hojeIso, isoParaBr } from '../lib/datas';
import { formatMoney } from '../lib/format';
import { useAppData } from '../state/AppData';
import { useCategoriasOrdenadas } from '../state/hooks';
import { useUi } from '../state/Ui';

function previewParcelas(tipo: string, parcelasTexto: string, valorTexto: string, data: string): string | null {
  const parcelas = parseInt(parcelasTexto) || 0;
  const valor = parseFloat(valorTexto) || 0;
  if (tipo !== 'Parcelado' || parcelas < 2 || !data) return null;
  const [y, m, d] = data.split('-').map(Number);
  // Mesma regra do backend: a última parcela cai no último dia do mês se preciso.
  const fim = adicionarMeses(new Date(y!, m! - 1, d), parcelas - 1);
  const rotuloFim = fim.toLocaleDateString('pt-BR', { month: 'short', year: '2-digit' });
  return `${parcelas}x de ${formatMoney(valor / parcelas)} · vai até ${rotuloFim}`;
}

export function Lancar() {
  const { recarregar } = useAppData();
  const { toast } = useUi();
  const categorias = useCategoriasOrdenadas();
  const [valor, setValor] = useState('');
  const [categoria, setCategoria] = useState('');
  const [descricao, setDescricao] = useState('');
  const [data, setData] = useState(hojeIso);
  const [tipo, setTipo] = useState('A vista');
  const [parcelas, setParcelas] = useState('2');
  const [observacao, setObservacao] = useState('');
  const [salvando, setSalvando] = useState(false);

  const preview = previewParcelas(tipo, parcelas, valor, data);

  const salvar = async () => {
    const valorNum = parseFloat(valor);
    const nParcelas = tipo === 'Parcelado' ? (parseInt(parcelas) || 1) : 1;

    // Valor negativo = crédito na fatura (aceito). Só vazio/0 é rejeitado.
    if (!valorNum) { toast('Informe um valor válido', 'error'); return; }
    if (!categoria) { toast('Selecione uma categoria', 'error'); return; }
    if (!data) { toast('Informe a data', 'error'); return; }
    if (nParcelas < 1 || nParcelas > 48) { toast('Parcelas entre 2 e 48', 'error'); return; }

    setSalvando(true);
    try {
      const resultado = await api.criarLancamento({
        data: isoParaBr(data), descricao: descricao.trim(), categoria, valor: valorNum,
        observacao: observacao.trim(), tipo, parcelas: nParcelas,
      });
      await recarregar();
      toast(resultado.inserted > 1 ? `${resultado.inserted} parcelas salvas! ✓` : 'Lançamento salvo! ✓', 'success');
      setValor('');
      setCategoria('');
      setDescricao('');
      setObservacao('');
      setTipo('A vista');
      setData(hojeIso());
    } catch (e) {
      toast(`Erro ao salvar: ${(e as Error).message}`, 'error');
    } finally {
      setSalvando(false);
    }
  };

  return (
    <div className="card">
      <div className="card-title">Novo lançamento</div>

      <div className="form-group">
        <label className="form-label" htmlFor="f-valor">Valor</label>
        <div className="value-input-wrap">
          <span className="value-prefix">R$</span>
          <input className="form-input" id="f-valor" type="number" inputMode="decimal" placeholder="0,00" step="0.01" value={valor} onChange={e => setValor(e.target.value)} />
        </div>
      </div>

      <div className="form-group">
        <label className="form-label" htmlFor="f-categoria">Categoria</label>
        <select className="form-select" id="f-categoria" value={categoria} onChange={e => setCategoria(e.target.value)}>
          <option value="">Selecione...</option>
          {categorias.map(n => <option key={n}>{n}</option>)}
        </select>
      </div>

      <div className="form-group">
        <label className="form-label" htmlFor="f-descricao">Descrição</label>
        <input className="form-input" id="f-descricao" type="text" placeholder="Ex: Almoço no Outback" value={descricao} onChange={e => setDescricao(e.target.value)} />
      </div>

      <div className="form-row">
        <div className="form-group">
          <label className="form-label" htmlFor="f-data">Data</label>
          <input className="form-input" id="f-data" type="date" value={data} onChange={e => setData(e.target.value)} />
        </div>
        <div className="form-group">
          <label className="form-label" htmlFor="f-tipo">Tipo</label>
          <select className="form-select" id="f-tipo" value={tipo} onChange={e => setTipo(e.target.value)}>
            <option>A vista</option>
            <option>Parcelado</option>
          </select>
        </div>
      </div>

      {tipo === 'Parcelado' && (
        <div className="form-group">
          <label className="form-label" htmlFor="f-parcelas">Número de parcelas</label>
          <input className="form-input" id="f-parcelas" type="number" inputMode="numeric" placeholder="Ex: 7" min="2" max="48" value={parcelas} onChange={e => setParcelas(e.target.value)} />
          {preview && <div className="parcel-info" style={{ marginTop: 8 }}>{preview}</div>}
        </div>
      )}

      <div className="form-group">
        <label className="form-label" htmlFor="f-estab">Observação <span style={{ color: 'var(--muted)', fontWeight: 400 }}>(opcional)</span></label>
        <input className="form-input" id="f-estab" type="text" placeholder="Ex: Pneus do TCross, aniversário da Laura..." value={observacao} onChange={e => setObservacao(e.target.value)} />
      </div>

      <button className="btn-primary" onClick={salvar} disabled={salvando}>
        {salvando ? 'Salvando...' : 'Salvar lançamento'}
      </button>
    </div>
  );
}
