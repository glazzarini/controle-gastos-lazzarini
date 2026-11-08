import { render, screen, waitFor, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { gerarMeses, MESES } from '../lib/meses';
import { AppDataProvider } from '../state/AppData';
import { NavegacaoContext } from '../state/Navegacao';
import { UiProvider } from '../state/Ui';
import { banco, falhar, lancamento } from '../test/servidor';
import { brl, renderApp, tela, toast } from '../test/render';
import { Historico } from './Historico';

async function abrirHistorico() {
  const r = await renderApp();
  await r.usuario.click(screen.getByRole('button', { name: /^Histórico/ }));
  return { ...r, h: tela('historico') };
}
const mes = (label: string) => tela('historico').getAllByTestId('hist-month').find(e => e.querySelector('.hist-label')!.textContent!.startsWith(label))!;

describe('Histórico', () => {
  it('mostra o mês atual (mesmo vazio) e os meses com lançamentos, com badges', async () => {
    lancamento({ data: '01/07/2026', valor: 1000, categoria: 'Pet' }); // 8% → Dentro da meta
    lancamento({ data: '01/08/2026', valor: 9500, categoria: 'Pet' }); // 79% → Atenção
    lancamento({ data: '01/09/2026', valor: 20000, categoria: 'Pet' }); // 167% → Acima da meta
    const { h } = await abrirHistorico();

    expect(h.getAllByTestId('hist-month').map(e => e.querySelector('.hist-label')!.textContent)).toEqual(['jun/26 atual', 'jul/26', 'ago/26', 'set/26']);
    expect(mes('jun/26')).toHaveTextContent('Sem dados');
    expect(mes('jun/26').getAttribute('style')).toContain('var(--accent)');
    expect(mes('jul/26')).toHaveTextContent('Dentro da meta');
    expect(mes('ago/26')).toHaveTextContent('Atenção');
    expect(mes('set/26')).toHaveTextContent('Acima da meta');
    expect((mes('set/26').querySelector('.hist-bar') as HTMLElement).style.width).toBe('100%');
    expect(mes('jul/26').getAttribute('style')).toBeNull();
  });

  it('detalhes: meta, fixos, renda, sobra com sinal e % e categorias com gasto', async () => {
    banco.categorias.push({ id: 'cat-0', nome: 'Zerada', teto: 0, created_at: null });
    lancamento({ data: '01/06/2026', valor: 1000, categoria: 'Pet' }); // 222% do teto
    lancamento({ data: '02/06/2026', valor: 1600, categoria: 'Alimentacao fora' }); // 80%
    lancamento({ data: '03/06/2026', valor: 10, categoria: 'Guitarra' }); // 3%
    lancamento({ data: '04/06/2026', valor: 5, categoria: 'Zerada' }); // teto 0
    lancamento({ data: '05/06/2026', valor: 7, categoria: 'Sem teto' });
    const { h } = await abrirHistorico();
    const junho = within(mes('jun/26'));

    expect(junho.getByText('🎯 Meta fatura').nextSibling).toHaveTextContent(brl(12000));
    expect(junho.getByText('🏠 Total fixos').nextSibling).toHaveTextContent(brl(1724));
    expect(junho.getByText('💼 Renda familiar').nextSibling).toHaveTextContent(brl(34000));
    const sobra = junho.getByText('💰 Sobra / Poupança').nextSibling as HTMLElement;
    expect(sobra).toHaveTextContent(`+${brl(29654)} 87.2%`);
    expect(sobra.getAttribute('style')).toContain('var(--green)');

    const linhas = junho.getAllByText((_, e) => e?.classList.contains('hist-cat-row') ?? false);
    expect(linhas.map(l => l.querySelector('.hist-cat-name')!.textContent)).toEqual(['🍽 Alimentacao fora', '🎸 Guitarra', '🐾 Pet', '💳 Zerada', '💳 Sem teto']);
    const cor = (i: number) => (linhas[i]!.querySelector('.hist-cat-val') as HTMLElement).style.color;
    expect([cor(0), cor(1), cor(2), cor(3), cor(4)]).toEqual(['var(--amber)', 'var(--green)', 'var(--red)', 'var(--green)', 'var(--green)']);
    expect(linhas[4]).toHaveTextContent(`${brl(7)} / ${brl(0)}`);
    expect(h).toBeDefined();
  });

  it('sobra negativa sem "+" e em vermelho; mês só com crédito não lista categorias', async () => {
    banco.config = { renda_guilherme: 1000, renda_esposa: 0, meta_fatura: 12000 };
    lancamento({ data: '01/06/2026', valor: 2000, categoria: 'Pet' });
    lancamento({ data: '01/07/2026', valor: -50, categoria: 'Pet' });
    await abrirHistorico();
    const sobra = within(mes('jun/26')).getByText('💰 Sobra / Poupança').nextSibling as HTMLElement;
    expect(sobra).toHaveTextContent(`${brl(-2724)} -272.4%`);
    expect(sobra.textContent!.startsWith('+')).toBe(false);
    expect(sobra.getAttribute('style')).toContain('var(--red)');
    expect(within(mes('jul/26')).queryByText('Por categoria')).not.toBeInTheDocument();
    expect((mes('jul/26').querySelector('.hist-bar') as HTMLElement).style.width).toBe('0%');
  });

  it('limpar mês sem lançamentos mostra erro', async () => {
    const { usuario } = await abrirHistorico();
    await usuario.click(within(mes('jun/26')).getByRole('button', { name: /Limpar todos os lançamentos de jun\/26/ }));
    expect(toast()).toHaveTextContent('Nenhum lançamento neste mês');
    expect(toast()).toHaveClass('error');
  });

  it('limpar mês exclui todos os lançamentos do mês após confirmação', async () => {
    lancamento({ data: '01/07/2026', valor: 10, categoria: 'Pet' });
    lancamento({ data: '02/07/2026', valor: 20, categoria: 'Pet' });
    lancamento({ data: '01/08/2026', valor: 30, categoria: 'Pet' });
    const { usuario } = await abrirHistorico();
    await usuario.click(within(mes('jul/26')).getByRole('button', { name: /Limpar/ }));
    expect(screen.getByRole('dialog')).toHaveTextContent('Limpar jul/26Isso vai excluir todos os 2 lançamento(s) de jul/26. Essa ação não pode ser desfeita.');
    await usuario.click(screen.getByRole('button', { name: 'Confirmar' }));
    await waitFor(() => expect(toast()).toHaveTextContent('jul/26 limpo ✓'));
    expect(banco.lancamentos.map(l => l.data)).toEqual(['01/08/2026']);
    expect(tela('historico').getAllByTestId('hist-month')).toHaveLength(2);
  });

  it('erro ao limpar mês', async () => {
    lancamento({ data: '01/07/2026', valor: 10, categoria: 'Pet' });
    falhar('delete', '/api/lancamentos/:id', 500, 'db down');
    const { usuario } = await abrirHistorico();
    await usuario.click(within(mes('jul/26')).getByRole('button', { name: /Limpar/ }));
    await usuario.click(screen.getByRole('button', { name: 'Confirmar' }));
    await waitFor(() => expect(toast()).toHaveTextContent('Erro ao limpar mês: db down'));
  });

  it('sem dados em nenhum mês da lista mostra o vazio', async () => {
    const navegacao = { tela: 'historico' as const, mes: MESES[0]!, irPara: () => {}, abrirDetalhe: () => {} };
    render(
      <UiProvider><AppDataProvider><NavegacaoContext.Provider value={navegacao}>
        <Historico meses={gerarMeses(new Date(2030, 0, 1), new Date(2030, 1, 1))} onLimparMes={() => {}} />
      </NavegacaoContext.Provider></AppDataProvider></UiProvider>);
    expect(await screen.findByText('Nenhum dado ainda. Comece lançando suas compras!')).toBeInTheDocument();
  });
});
