import type { Config, Lancamento } from './types';
import type { Mes } from './meses';
import { META_FATURA_PADRAO } from './defaults';

export function lancamentosDoMes(lancamentos: Lancamento[], mes: Mes): Lancamento[] {
  return lancamentos.filter(l =>
    l.data.getMonth() === mes.date.getMonth() && l.data.getFullYear() === mes.date.getFullYear());
}

/** Total por categoria: começa com todas as categorias com teto (0) e soma os lançamentos (créditos abatem). */
export function totaisPorCategoria(lancamentos: Lancamento[], tetos: Record<string, number>): Record<string, number> {
  const totais: Record<string, number> = {};
  Object.keys(tetos).forEach(k => { totais[k] = 0; });
  lancamentos.forEach(l => {
    if (totais[l.categoria] !== undefined) totais[l.categoria]! += l.valor;
    else if (l.categoria) totais[l.categoria] = l.valor;
  });
  return totais;
}

export function soma(valores: number[]): number {
  return valores.reduce((a, b) => a + b, 0);
}

/** meta_fatura || 12000 (meta 0 evitaria divisão por zero nas porcentagens). */
export function metaFatura(config: Config): number {
  return config.meta_fatura || META_FATURA_PADRAO;
}

/** Renda familiar — respeita renda 0 (o index.html usava `|| 20000`). */
export function rendaTotal(config: Config): number {
  return config.renda_guilherme + config.renda_esposa;
}

export interface ResumoMes {
  totalFatura: number;
  renda: number;
  fixos: number;
  sobra: number;
  pctPoupanca: number;
}

export function resumoOrcamento(config: Config, totalFatura: number): ResumoMes {
  const renda = rendaTotal(config);
  const fixos = config.total_fixos;
  const sobra = renda - fixos - totalFatura;
  return { totalFatura, renda, fixos, sobra, pctPoupanca: renda > 0 ? sobra / renda : 0 };
}
