import { describe, expect, it } from 'vitest';
import { formatDate, formatMoney, formatValorLancamento, percentual } from './format';

const nbsp = ' ';

describe('format', () => {
  it('formatMoney em BRL; vazio vira 0', () => {
    expect(formatMoney(1234.5)).toBe(`R$${nbsp}1.234,50`);
    expect(formatMoney(null)).toBe(`R$${nbsp}0,00`);
    expect(formatMoney(undefined)).toBe(`R$${nbsp}0,00`);
    expect(formatMoney(-50)).toBe(`-R$${nbsp}50,00`);
  });

  it('formatDate DD/MM/YYYY', () => expect(formatDate(new Date(2026, 5, 5))).toBe('05/06/2026'));

  it('formatValorLancamento: débito com "-" e crédito com "+"', () => {
    expect(formatValorLancamento(10)).toBe(`-R$${nbsp}10,00`);
    expect(formatValorLancamento(-75.5)).toBe(`+R$${nbsp}75,50`);
  });

  it('percentual', () => {
    expect(percentual(0.756, 0)).toBe('76%');
    expect(percentual(0.2, 1)).toBe('20.0%');
  });
});
