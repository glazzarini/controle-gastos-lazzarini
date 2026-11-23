import { describe, expect, it } from 'vitest';
import { gerarMeses, MESES, mesCorrente } from './meses';

describe('meses', () => {
  it('vai de jun/26 a dez/28 (31 meses)', () => {
    expect(MESES).toHaveLength(31);
    expect(MESES[0]!.label).toBe('jun/26');
    expect(MESES.at(-1)!.label).toBe('dez/28');
    expect(MESES.map(m => m.label)).toContain('jan/27');
    expect(MESES[7]).toEqual({ label: 'jan/27', date: new Date(2027, 0, 1) });
  });

  it('gerarMeses com faixa customizada', () => {
    expect(gerarMeses(new Date(2026, 10, 20), new Date(2027, 1, 1)).map(m => m.label)).toEqual(['nov/26', 'dez/26', 'jan/27', 'fev/27']);
  });

  it('mesCorrente: mês de hoje', () => {
    expect(mesCorrente().label).toBe('jun/26');
    expect(mesCorrente(MESES, new Date(2026, 10, 7)).label).toBe('nov/26');
  });

  it('mesCorrente: fora da faixa usa o limite mais próximo', () => {
    expect(mesCorrente(MESES, new Date(2025, 0, 1)).label).toBe('jun/26');
    expect(mesCorrente(MESES, new Date(2030, 0, 1)).label).toBe('dez/28');
  });
});
