import { describe, expect, it } from 'vitest';
import { lancamentosDoMes, metaFatura, rendaTotal, resumoOrcamento, soma, totaisPorCategoria } from './calculos';
import { MESES } from './meses';
import type { Config, Lancamento } from './types';

const l = (data: Date, categoria: string, valor: number): Lancamento =>
  ({ id: `${categoria}${valor}`, data, categoria, valor, descricao: '', observacao: '', tipo: 'A vista' });

const config = (parcial: Partial<Config> = {}): Config =>
  ({ renda_guilherme: 20000, renda_esposa: 14000, meta_fatura: 12000, total_fixos: 14559, ...parcial });

describe('calculos', () => {
  it('lancamentosDoMes filtra por mês e ano', () => {
    const lancs = [l(new Date(2026, 5, 1), 'Pet', 1), l(new Date(2027, 5, 1), 'Pet', 2), l(new Date(2026, 6, 1), 'Pet', 3)];
    expect(lancamentosDoMes(lancs, MESES[0]!).map(x => x.valor)).toEqual([1]);
  });

  it('totaisPorCategoria: inclui categorias sem lançamento, categorias sem teto e abate créditos', () => {
    const totais = totaisPorCategoria(
      [l(new Date(), 'Pet', 100), l(new Date(), 'Pet', -30), l(new Date(), 'Sem teto', 5), l(new Date(), 'Sem teto', 5), l(new Date(), '', 9)],
      { Pet: 450, Guitarra: 300 });
    expect(totais).toEqual({ Pet: 70, Guitarra: 0, 'Sem teto': 10 });
  });

  it('soma', () => expect(soma([1, 2.5, -1])).toBe(2.5));

  it('metaFatura: 0 usa 12000', () => {
    expect(metaFatura(config())).toBe(12000);
    expect(metaFatura(config({ meta_fatura: 0 }))).toBe(12000);
    expect(metaFatura(config({ meta_fatura: 8000 }))).toBe(8000);
  });

  it('rendaTotal respeita renda 0', () => expect(rendaTotal(config({ renda_guilherme: 0, renda_esposa: 0 }))).toBe(0));

  it('resumoOrcamento', () => {
    expect(resumoOrcamento(config(), 1000)).toEqual({ totalFatura: 1000, renda: 34000, fixos: 14559, sobra: 18441, pctPoupanca: 18441 / 34000 });
    expect(resumoOrcamento(config({ renda_guilherme: 0, renda_esposa: 0, total_fixos: 0 }), 10).pctPoupanca).toBe(0);
  });
});
