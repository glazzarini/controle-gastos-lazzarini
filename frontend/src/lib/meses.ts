export interface Mes {
  label: string;
  date: Date;
}

const NOMES = ['jan', 'fev', 'mar', 'abr', 'mai', 'jun', 'jul', 'ago', 'set', 'out', 'nov', 'dez'];

/** Meses disponíveis: de jun/26 a dez/28. */
export const INICIO_MESES = new Date(2026, 5, 1);
export const FIM_MESES = new Date(2028, 11, 1);

export function gerarMeses(inicio: Date = INICIO_MESES, fim: Date = FIM_MESES): Mes[] {
  const meses: Mes[] = [];
  for (let d = new Date(inicio.getFullYear(), inicio.getMonth(), 1); d <= fim; d = new Date(d.getFullYear(), d.getMonth() + 1, 1)) {
    meses.push({ label: `${NOMES[d.getMonth()]}/${String(d.getFullYear()).slice(-2)}`, date: d });
  }
  return meses;
}

export const MESES = gerarMeses();

/** Mês corrente; fora da faixa usa o limite mais próximo. */
export function mesCorrente(meses: Mes[] = MESES, hoje: Date = new Date()): Mes {
  const atual = meses.find(m => m.date.getFullYear() === hoje.getFullYear() && m.date.getMonth() === hoje.getMonth());
  if (atual) return atual;
  return hoje < meses[0]!.date ? meses[0]! : meses[meses.length - 1]!;
}
