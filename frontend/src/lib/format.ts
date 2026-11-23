const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

export function formatMoney(valor: number | null | undefined): string {
  return moeda.format(valor || 0);
}

export function formatDate(data: Date): string {
  return data.toLocaleDateString('pt-BR');
}

/** Valor de um lançamento: débito "-R$ 10,00"; crédito (valor negativo) "+R$ 10,00". */
export function formatValorLancamento(valor: number): string {
  return valor < 0 ? `+${formatMoney(Math.abs(valor))}` : `-${formatMoney(valor)}`;
}

export function percentual(fracao: number, casas: number): string {
  return `${(fracao * 100).toFixed(casas)}%`;
}
