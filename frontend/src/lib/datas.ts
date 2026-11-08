const doisDigitos = (n: number) => String(n).padStart(2, '0');

/** YYYY-MM-DD em horário local (o index.html usava UTC e "pulava" para amanhã à noite). */
export function dataParaIso(data: Date): string {
  return `${data.getFullYear()}-${doisDigitos(data.getMonth() + 1)}-${doisDigitos(data.getDate())}`;
}

export function hojeIso(): string {
  return dataParaIso(new Date());
}

/** YYYY-MM-DD (input date) → DD/MM/YYYY (contrato da API). */
export function isoParaBr(iso: string): string {
  const [y, m, d] = iso.split('-');
  return `${d}/${m}/${y}`;
}

/** DD/MM/YYYY ou ISO → Date local. Sem valor devolve "agora" (como o parseDate original). */
export function parseDate(texto: string | null): Date {
  if (!texto) return new Date();
  if (texto.includes('/')) {
    const [d, m, y] = texto.split('/').map(Number);
    return new Date(y!, m! - 1, d);
  }
  return new Date(texto);
}

/** Soma meses ajustando para o último dia do mês (31/01 + 1 → 28/02), igual ao backend. */
export function adicionarMeses(data: Date, meses: number): Date {
  const alvo = new Date(data.getFullYear(), data.getMonth() + meses, 1);
  const ultimoDia = new Date(alvo.getFullYear(), alvo.getMonth() + 1, 0).getDate();
  alvo.setDate(Math.min(data.getDate(), ultimoDia));
  return alvo;
}
