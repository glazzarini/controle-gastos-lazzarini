import { useState } from 'react';
import { EditLancamentoModal } from '../components/EditLancamentoModal';
import { IconeEditar, IconeLixeira, IconeVoltar } from '../components/Icones';
import { iconeCategoria } from '../lib/defaults';
import { formatDate, formatValorLancamento } from '../lib/format';
import type { Lancamento } from '../lib/types';
import { useAppData } from '../state/AppData';
import { useCorLancamento } from '../state/hooks';
import { useNavegacao } from '../state/Navegacao';

interface Props {
  id: string | null;
  onExcluir: (l: Lancamento) => void;
}

export function DetalheLancamento({ id, onExcluir }: Props) {
  const { lancamentos } = useAppData();
  const { irPara } = useNavegacao();
  const cor = useCorLancamento();
  const [editando, setEditando] = useState<Lancamento | null>(null);

  const l = lancamentos.find(x => x.id === id);
  if (!l) return null;

  return (
    <>
      <button className="back-btn" onClick={() => irPara('lancamentos')}>
        <IconeVoltar />
        Voltar
      </button>
      <div className="detail-card">
        <div className="detail-hero">
          <div className="detail-hero-icon">{iconeCategoria(l.categoria)}</div>
          <div className="detail-hero-desc">{l.descricao || '(sem descrição)'}</div>
          <div className="detail-hero-cat">{l.categoria}</div>
          <div className="detail-hero-value" style={{ color: cor(l) }}>{formatValorLancamento(l.valor)}</div>
        </div>
        <div className="detail-row">
          <span className="detail-row-label">📅 Data</span>
          <span className="detail-row-value">{formatDate(l.data)}</span>
        </div>
        <div className="detail-row">
          <span className="detail-row-label">💳 Tipo</span>
          <span className="detail-row-value">{l.tipo}</span>
        </div>
        {l.observacao && (
          <div className="detail-row">
            <span className="detail-row-label">📝 Observação</span>
            <span className="detail-row-value">{l.observacao}</span>
          </div>
        )}
      </div>
      <button className="detail-edit-btn" onClick={() => setEditando(l)}>
        <IconeEditar />
        Editar lançamento
      </button>
      <button className="detail-delete-btn" onClick={() => onExcluir(l)}>
        <IconeLixeira width={16} height={16} strokeWidth={2} />
        Excluir lançamento
      </button>
      <EditLancamentoModal lancamento={editando} onFechar={() => setEditando(null)} />
    </>
  );
}
