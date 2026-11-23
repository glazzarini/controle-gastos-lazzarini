import { describe, expect, it } from 'vitest';
import { classeProgresso, corFaturaOrcamento, corFaturaPainel, corPorFracao, corPoupanca, corSinal, corValorLancamento, variavelCss } from './cores';
import { iconeCategoria } from './defaults';

describe('cores', () => {
  it('corFaturaPainel', () => {
    expect([0.74, 0.75, 0.99, 1].map(corFaturaPainel)).toEqual(['green', 'amber', 'amber', 'red']);
  });

  it('corPorFracao', () => {
    expect([0.74, 0.75, 1, 1.01].map(corPorFracao)).toEqual(['green', 'amber', 'amber', 'red']);
  });

  it('corFaturaOrcamento', () => {
    expect([9000, 9001, 12000, 12001].map(t => corFaturaOrcamento(t, 12000))).toEqual(['green', 'amber', 'amber', 'red']);
  });

  it('classeProgresso', () => expect([0.5, 0.75, 1, 1.2].map(classeProgresso)).toEqual(['ok', 'warn', 'warn', 'over']));

  it('corSinal e corPoupanca', () => {
    expect([0, -1].map(corSinal)).toEqual(['green', 'red']);
    expect([0.2, 0.1, 0.09].map(corPoupanca)).toEqual(['green', 'amber', 'red']);
  });

  it('corValorLancamento: crédito verde; débito pela categoria; sem teto ou gasto cinza', () => {
    expect(corValorLancamento(-10, 500, 450)).toBe('var(--green)');
    expect(corValorLancamento(10, 0, 450)).toBe('var(--muted)');
    expect(corValorLancamento(10, 10, 0)).toBe('var(--muted)');
    expect(corValorLancamento(10, 500, 450)).toBe('var(--red)');
    expect(corValorLancamento(10, 400, 450)).toBe(variavelCss('amber'));
    expect(corValorLancamento(10, 10, 450)).toBe('var(--green)');
  });

  it('iconeCategoria', () => {
    expect(iconeCategoria('Pet')).toBe('🐾');
    expect(iconeCategoria('Desconhecida')).toBe('💳');
  });
});
