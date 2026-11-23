import { iconeCategoria } from '../lib/defaults';
import { formatDate, formatValorLancamento } from '../lib/format';
import type { Lancamento } from '../lib/types';
import { useCorLancamento } from '../state/hooks';
import { IconeLixeira } from './Icones';

interface Props {
  lancamento: Lancamento;
  /** Na tela Lançamentos o item abre o detalhe e tem lixeira; no Painel é só exibição. */
  onAbrir?: (l: Lancamento) => void;
  onExcluir?: (l: Lancamento) => void;
}

export function LancItem({ lancamento: l, onAbrir, onExcluir }: Props) {
  const cor = useCorLancamento();
  return (
    <div
      className="lanc-item"
      onClick={onAbrir ? () => onAbrir(l) : undefined}
      style={onAbrir ? { cursor: 'pointer' } : undefined}
      data-testid="lanc-item"
    >
      <div className="lanc-icon">{iconeCategoria(l.categoria)}</div>
      <div className="lanc-info">
        <div className="lanc-desc">{l.descricao || l.categoria}</div>
        <div className="lanc-cat">{l.categoria}{l.observacao ? ` · ${l.observacao}` : ''}</div>
      </div>
      <div className="lanc-right">
        <div className="lanc-value" style={{ color: cor(l) }}>{formatValorLancamento(l.valor)}</div>
        <div className="lanc-date">{formatDate(l.data)}</div>
      </div>
      {onExcluir && (
        <button
          className="lanc-delete"
          title="Excluir"
          aria-label="Excluir"
          onClick={e => { e.stopPropagation(); onExcluir(l); }}
        >
          <IconeLixeira />
        </button>
      )}
    </div>
  );
}
