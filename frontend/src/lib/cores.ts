// Regras de cor do index.html, uma função por regra (mantendo os limites exatos de cada tela).

export type Cor = 'green' | 'amber' | 'red';
export type ClasseProgresso = 'ok' | 'warn' | 'over';

/** Painel — "Gasto no mês" (fração já limitada a 1): < 75% verde, < 100% amarelo, senão vermelho. */
export function corFaturaPainel(fracao: number): Cor {
  return fracao < 0.75 ? 'green' : fracao < 1 ? 'amber' : 'red';
}

/** Lançamentos/Histórico: > 100% vermelho, ≥ 75% amarelo, senão verde. */
export function corPorFracao(fracao: number): Cor {
  return fracao > 1 ? 'red' : fracao >= 0.75 ? 'amber' : 'green';
}

/** Orçamento — "Fatura cartão": acima da meta vermelho, acima de 75% amarelo. */
export function corFaturaOrcamento(total: number, meta: number): Cor {
  return total > meta ? 'red' : total > meta * 0.75 ? 'amber' : 'green';
}

export function classeProgresso(fracao: number): ClasseProgresso {
  return fracao > 1 ? 'over' : fracao >= 0.75 ? 'warn' : 'ok';
}

/** Sobra: positivo verde, negativo vermelho. */
export function corSinal(valor: number): Cor {
  return valor >= 0 ? 'green' : 'red';
}

/** % Poupança: ≥ 20% verde, ≥ 10% amarelo, < 10% vermelho. */
export function corPoupanca(fracao: number): Cor {
  return fracao >= 0.2 ? 'green' : fracao >= 0.1 ? 'amber' : 'red';
}

export function variavelCss(cor: Cor): string {
  return `var(--${cor})`;
}

/**
 * Cor do valor de um lançamento: crédito (valor < 0) sempre verde; débito segue o consumo
 * da categoria no mês (cinza sem teto ou sem gasto).
 */
export function corValorLancamento(valor: number, gastoCategoria: number, teto: number): string {
  if (valor < 0) return variavelCss('green');
  if (teto === 0 || gastoCategoria === 0) return 'var(--muted)';
  return variavelCss(corPorFracao(gastoCategoria / teto));
}
