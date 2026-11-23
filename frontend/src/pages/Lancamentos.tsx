import { Carregando, Vazio } from '../components/Carregando';
import { LancItem } from '../components/LancItem';
import { metaFatura, soma } from '../lib/calculos';
import { corPorFracao } from '../lib/cores';
import { CAT_ICONS } from '../lib/defaults';
import { formatMoney } from '../lib/format';
import type { Lancamento } from '../lib/types';
import { useAppData } from '../state/AppData';
import { useMesAtual } from '../state/hooks';
import { useNavegacao } from '../state/Navegacao';

interface Props {
  filtro: string;
  onFiltrar: (categoria: string) => void;
  onExcluir: (l: Lancamento) => void;
}

export function Lancamentos({ filtro, onFiltrar, onExcluir }: Props) {
  const { config, tetos, carregado } = useAppData();
  const { mes, abrirDetalhe } = useNavegacao();
  const { doMes } = useMesAtual();

  if (!carregado) return <Carregando />;
  if (doMes.length === 0) return <Vazio icone="📋" texto={`Nenhum lançamento em ${mes.label}`} />;

  const total = soma(doMes.map(l => l.valor));
  const cor = corPorFracao(total / metaFatura(config));

  // Chips: categorias com teto que têm lançamentos no mês + categorias sem teto.
  const usadas = ['Todos', ...Object.keys(tetos).filter(c => doMes.some(l => l.categoria === c))];
  doMes.forEach(l => { if (l.categoria && !usadas.includes(l.categoria)) usadas.push(l.categoria); });

  const filtrados = filtro === 'Todos' ? doMes : doMes.filter(l => l.categoria === filtro);

  return (
    <>
      <div className="metric-row">
        <div className="metric">
          <div className="metric-label">Total lançado</div>
          <div className={`metric-value ${cor}`}>{formatMoney(total)}</div>
          <div className="metric-sub">{doMes.length} lançamento{doMes.length !== 1 ? 's' : ''}</div>
        </div>
      </div>

      <div className="filter-bar">
        {usadas.map(c => (
          <div key={c} role="button" className={`filter-chip${c === filtro ? ' active' : ''}`} onClick={() => onFiltrar(c)}>
            {c === 'Todos' ? '' : `${CAT_ICONS[c] || ''} `}{c}
          </div>
        ))}
      </div>

      {filtrados.length === 0
        ? <Vazio icone="🔍" texto="Nenhum lançamento nesta categoria" />
        : (
          <div className="card">
            {filtrados.map(l => <LancItem key={l.id} lancamento={l} onAbrir={x => abrirDetalhe(x.id)} onExcluir={onExcluir} />)}
          </div>
        )}
    </>
  );
}
