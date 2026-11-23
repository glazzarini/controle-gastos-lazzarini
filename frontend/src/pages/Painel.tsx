import { Carregando } from '../components/Carregando';
import { LancItem } from '../components/LancItem';
import { metaFatura, soma } from '../lib/calculos';
import { classeProgresso, corFaturaPainel, corSinal } from '../lib/cores';
import { iconeCategoria } from '../lib/defaults';
import { formatMoney, percentual } from '../lib/format';
import { useAppData } from '../state/AppData';
import { useMesAtual } from '../state/hooks';
import { useNavegacao } from '../state/Navegacao';

function StatusCategoria({ gasto, fracao }: { gasto: number; fracao: number }) {
  if (gasto === 0) return null;
  if (fracao > 1) return <span style={{ fontSize: 10, color: 'var(--red)' }}>🔴 Estourou</span>;
  if (fracao >= 0.75) return <span style={{ fontSize: 10, color: 'var(--amber)' }}>🟡 Atenção</span>;
  return <span style={{ fontSize: 10, color: 'var(--green)' }}>✅ OK</span>;
}

export function Painel() {
  const { config, tetos, carregado } = useAppData();
  const { irPara } = useNavegacao();
  const { doMes, totais } = useMesAtual();

  if (!carregado) return <Carregando />;

  const totalFatura = soma(Object.values(totais));
  const meta = metaFatura(config);
  const fracaoFatura = Math.min(totalFatura / meta, 1);
  const diferenca = meta - totalFatura;

  return (
    <>
      <div className="metric-row">
        <div className="metric">
          <div className="metric-label">Gasto no mês</div>
          <div className={`metric-value ${corFaturaPainel(fracaoFatura)}`}>{formatMoney(totalFatura)}</div>
          <div className="metric-sub">{percentual(fracaoFatura, 0)} da meta</div>
        </div>
        <div className="metric">
          <div className="metric-label">{diferenca >= 0 ? 'Disponível' : 'Acima da meta'}</div>
          <div className={`metric-value ${corSinal(diferenca)}`}>{formatMoney(Math.abs(diferenca))}</div>
          <div className="metric-sub">meta {formatMoney(meta)}</div>
        </div>
      </div>

      <div className="card">
        <div className="card-title">Por categoria</div>
        {Object.entries(tetos).map(([categoria, teto]) => {
          const gasto = totais[categoria] || 0;
          const fracao = teto > 0 ? Math.min(gasto / teto, 1.2) : 0;
          // Créditos podem deixar a categoria negativa: a barra não fica abaixo de 0%.
          const largura = teto > 0 ? Math.max(0, Math.min((gasto / teto) * 100, 100)) : 0;
          return (
            <div className="cat-item" key={categoria} data-testid="cat-item">
              <div className="cat-header">
                <div className="cat-name">{iconeCategoria(categoria)} {categoria}</div>
                <div className="cat-values"><span>{formatMoney(gasto)}</span> / {formatMoney(teto)}</div>
              </div>
              <div className="progress-track">
                <div className={`progress-fill ${classeProgresso(fracao)}`} style={{ width: `${largura}%` }} />
              </div>
              <StatusCategoria gasto={gasto} fracao={fracao} />
            </div>
          );
        })}
      </div>

      {doMes.length > 0 && (
        <div className="card">
          <div className="section-header">
            <div className="section-title">Últimos lançamentos</div>
            <div className="section-count">{doMes.length} no mês</div>
          </div>
          {doMes.slice(0, 5).map(l => <LancItem key={l.id} lancamento={l} />)}
          {doMes.length > 5 && (
            <div style={{ textAlign: 'center', paddingTop: 10 }}>
              <button
                onClick={() => irPara('lancamentos')}
                style={{ background: 'none', border: 'none', color: 'var(--accent)', fontSize: 13, cursor: 'pointer', fontFamily: "'DM Sans',sans-serif" }}
              >
                Ver todos ({doMes.length}) →
              </button>
            </div>
          )}
        </div>
      )}
    </>
  );
}
