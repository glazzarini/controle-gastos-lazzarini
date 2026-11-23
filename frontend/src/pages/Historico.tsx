import { Carregando, Vazio } from '../components/Carregando';
import { IconeLixeira } from '../components/Icones';
import { lancamentosDoMes, metaFatura, resumoOrcamento, soma, totaisPorCategoria } from '../lib/calculos';
import { corPorFracao, corSinal, variavelCss } from '../lib/cores';
import { iconeCategoria } from '../lib/defaults';
import { formatMoney, percentual } from '../lib/format';
import { MESES, type Mes } from '../lib/meses';
import { useAppData } from '../state/AppData';
import { useNavegacao } from '../state/Navegacao';

function Badge({ vazio, fracao }: { vazio: boolean; fracao: number }) {
  if (vazio) return <span className="hist-status-badge badge-empty">Sem dados</span>;
  if (fracao > 1) return <span className="hist-status-badge badge-over">Acima da meta</span>;
  if (fracao >= 0.75) return <span className="hist-status-badge badge-warn">Atenção</span>;
  return <span className="hist-status-badge badge-ok">Dentro da meta</span>;
}

interface Props {
  meses?: Mes[];
  onLimparMes: (mes: Mes) => void;
}

export function Historico({ meses = MESES, onLimparMes }: Props) {
  const { config, tetos, lancamentos, carregado } = useAppData();
  const { mes: atual } = useNavegacao();

  if (!carregado) return <Carregando />;

  const meta = metaFatura(config);
  const comDados = meses.filter(m => lancamentosDoMes(lancamentos, m).length > 0 || m.label === atual.label);

  if (comDados.length === 0) {
    return <Vazio icone="📅" texto="Nenhum dado ainda. Comece lançando suas compras!" />;
  }

  return (
    <>
      {comDados.map(m => {
        const doMes = lancamentosDoMes(lancamentos, m);
        const totais = totaisPorCategoria(doMes, tetos);
        const resumo = resumoOrcamento(config, soma(Object.values(totais)));
        const fracao = Math.min(resumo.totalFatura / meta, 1.5);
        const corFatura = variavelCss(corPorFracao(fracao));
        const corSobra = variavelCss(corSinal(resumo.sobra));
        const ehAtual = m.label === atual.label;
        const categoriasComGasto = Object.entries(totais).filter(([, v]) => v > 0);

        return (
          <div className="hist-month" key={m.label} style={ehAtual ? { borderColor: 'var(--accent)' } : undefined} data-testid="hist-month">
            <div className="hist-header">
              <div className="hist-label">
                {m.label}
                {ehAtual && <> <span style={{ fontSize: 10, color: 'var(--accent)', fontWeight: 400 }}>atual</span></>}
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <div className="hist-total" style={{ color: corFatura }}>{formatMoney(resumo.totalFatura)}</div>
                <Badge vazio={doMes.length === 0} fracao={fracao} />
              </div>
            </div>
            <div className="hist-bar-wrap">
              <div className="hist-bar" style={{ width: `${Math.max(0, Math.min(fracao * 100, 100))}%`, background: corFatura }} />
            </div>

            <div className="hist-expanded">
              <div className="hist-exp-row">
                <span className="hist-exp-label">🎯 Meta fatura</span>
                <span className="hist-exp-value" style={{ color: 'var(--accent)' }}>{formatMoney(meta)}</span>
              </div>
              <div className="hist-exp-row">
                <span className="hist-exp-label">🏠 Total fixos</span>
                <span className="hist-exp-value">{formatMoney(resumo.fixos)}</span>
              </div>
              <div className="hist-exp-row">
                <span className="hist-exp-label">💼 Renda familiar</span>
                <span className="hist-exp-value">{formatMoney(resumo.renda)}</span>
              </div>
              <div className="hist-exp-row" style={{ borderTop: '1px solid var(--border)', marginTop: 4, paddingTop: 8 }}>
                <span className="hist-exp-label" style={{ fontWeight: 600, color: 'var(--text)' }}>💰 Sobra / Poupança</span>
                <span className="hist-exp-value" style={{ color: corSobra, fontSize: 14 }}>
                  {resumo.sobra >= 0 ? '+' : ''}{formatMoney(resumo.sobra)} <span style={{ fontSize: 10, opacity: 0.7 }}>{percentual(resumo.pctPoupanca, 1)}</span>
                </span>
              </div>
              {categoriasComGasto.length > 0 && (
                <div className="hist-cats" style={{ marginTop: 10 }}>
                  <div style={{ fontSize: 10, color: 'var(--muted)', textTransform: 'uppercase', letterSpacing: '.05em', marginBottom: 6 }}>Por categoria</div>
                  {categoriasComGasto.map(([categoria, valor]) => {
                    const teto = tetos[categoria] || 0;
                    const cor = variavelCss(corPorFracao(teto > 0 ? valor / teto : 0));
                    return (
                      <div className="hist-cat-row" key={categoria}>
                        <span className="hist-cat-name">{iconeCategoria(categoria)} {categoria}</span>
                        <span className="hist-cat-val" style={{ color: cor }}>
                          {formatMoney(valor)} <span style={{ color: 'var(--muted)', fontSize: 10 }}>/ {formatMoney(teto)}</span>
                        </span>
                      </div>
                    );
                  })}
                </div>
              )}
              <button className="clear-month-btn" onClick={() => onLimparMes(m)}>
                <IconeLixeira width={14} height={14} strokeWidth={2} />
                Limpar todos os lançamentos de {m.label}
              </button>
            </div>
          </div>
        );
      })}
    </>
  );
}
