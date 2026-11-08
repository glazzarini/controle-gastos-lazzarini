import { describe, expect, it } from 'vitest';
import { adicionarMeses, dataParaIso, hojeIso, isoParaBr, parseDate } from './datas';

describe('datas', () => {
  it('hojeIso usa horário local (22:30 de 15/06 continua 15/06)', () => expect(hojeIso()).toBe('2026-06-15'));

  it('dataParaIso e isoParaBr', () => {
    expect(dataParaIso(new Date(2026, 0, 5))).toBe('2026-01-05');
    expect(isoParaBr('2026-01-05')).toBe('05/01/2026');
  });

  it('parseDate: DD/MM/YYYY, ISO e vazio', () => {
    expect(parseDate('05/06/2026')).toEqual(new Date(2026, 5, 5));
    expect(parseDate('2026-06-05T00:00:00')).toEqual(new Date(2026, 5, 5));
    expect(parseDate(null)).toEqual(new Date(2026, 5, 15, 22, 30));
  });

  it.each([
    [new Date(2026, 0, 31), 1, new Date(2026, 1, 28)],
    [new Date(2028, 0, 31), 1, new Date(2028, 1, 29)],
    [new Date(2026, 2, 31), 1, new Date(2026, 3, 30)],
    [new Date(2026, 11, 15), 1, new Date(2027, 0, 15)],
    [new Date(2026, 5, 10), 0, new Date(2026, 5, 10)],
  ])('adicionarMeses(%s, %i) ajusta para o último dia do mês', (inicio, meses, esperado) => {
    expect(adicionarMeses(inicio, meses)).toEqual(esperado);
  });
});
