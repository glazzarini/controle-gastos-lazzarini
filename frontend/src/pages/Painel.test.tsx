import { screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { banco, lancamento } from '../test/servidor';
import { brl, renderApp, tela } from '../test/render';

const categoria = (nome: string) => tela('painel').getAllByTestId('cat-item').find(e => e.textContent!.includes(nome))!;
const barra = (nome: string) => categoria(nome).querySelector('.progress-fill') as HTMLElement;

describe('Painel', () => {
  it('métricas do mês: gasto, % da meta e disponível (verde)', async () => {
    lancamento({ data: '05/06/2026', valor: 1000, categoria: 'Pet' });
    lancamento({ data: '05/07/2026', valor: 5000, categoria: 'Pet' }); // outro mês
    await renderApp();
    const painel = tela('painel');

    const gasto = painel.getByText('Gasto no mês').parentElement!;
    expect(within(gasto).getByText(brl(1000))).toHaveClass('metric-value', 'green');
    expect(gasto).toHaveTextContent('8% da meta');
    const disponivel = painel.getByText('Disponível').parentElement!;
    expect(within(disponivel).getByText(brl(11000))).toHaveClass('green');
    expect(disponivel).toHaveTextContent(`meta ${brl(12000)}`);
  });

  it('acima de 75% da meta fica amarelo; acima da meta vermelho com "Acima da meta"', async () => {
    lancamento({ data: '05/06/2026', valor: 9000, categoria: 'Pet' });
    await renderApp();
    expect(tela('painel').getByText(brl(9000), { selector: '.metric-value' })).toHaveClass('amber');
  });

  it('acima da meta', async () => {
    lancamento({ data: '05/06/2026', valor: 13000, categoria: 'Pet' });
    await renderApp();
    const painel = tela('painel');
    expect(painel.getByText(brl(13000), { selector: '.metric-value' })).toHaveClass('red');
    expect(painel.getByText('Gasto no mês').parentElement).toHaveTextContent('100% da meta');
    const acima = painel.getByText('Acima da meta').parentElement!;
    expect(within(acima).getByText(brl(1000))).toHaveClass('red');
  });

  it('meta_fatura 0 usa 12.000', async () => {
    banco.config.meta_fatura = 0;
    await renderApp();
    expect(tela('painel').getByText('Disponível').parentElement).toHaveTextContent(`meta ${brl(12000)}`);
  });

  it('barras por categoria: OK, Atenção, Estourou e sem gasto', async () => {
    lancamento({ data: '01/06/2026', valor: 100, categoria: 'Alimentacao fora' }); // 5%
    lancamento({ data: '01/06/2026', valor: 400, categoria: 'Pet' }); // 89%
    lancamento({ data: '01/06/2026', valor: 600, categoria: 'Guitarra' }); // 200%
    await renderApp();

    expect(categoria('Alimentacao fora')).toHaveTextContent(`🍽 Alimentacao fora${brl(100)} / ${brl(2000)}✅ OK`);
    expect(barra('Alimentacao fora')).toHaveClass('ok');
    expect(barra('Alimentacao fora').style.width).toBe('5%');
    expect(categoria('Pet')).toHaveTextContent('🟡 Atenção');
    expect(barra('Pet')).toHaveClass('warn');
    expect(categoria('Guitarra')).toHaveTextContent('🔴 Estourou');
    expect(barra('Guitarra')).toHaveClass('over');
    expect(barra('Guitarra').style.width).toBe('100%');
  });

  it('categoria sem gasto não mostra status; teto 0 tem barra vazia', async () => {
    banco.categorias.push({ id: 'cat-zero', nome: 'Zerada', teto: 0, created_at: null });
    lancamento({ data: '01/06/2026', valor: 50, categoria: 'Zerada' });
    await renderApp();
    expect(categoria('Pet')).not.toHaveTextContent(/OK|Atenção|Estourou/);
    expect(barra('Zerada').style.width).toBe('0%');
    expect(categoria('Zerada')).toHaveTextContent('💳 Zerada');
    expect(categoria('Zerada')).toHaveTextContent('✅ OK');
  });

  it('crédito abate do total e a barra não fica negativa', async () => {
    lancamento({ data: '01/06/2026', valor: 100, categoria: 'Pet' });
    lancamento({ data: '02/06/2026', valor: -300, categoria: 'Pet', descricao: 'Estorno' });
    await renderApp();
    expect(categoria('Pet')).toHaveTextContent(`${brl(-200)} / ${brl(450)}`);
    expect(barra('Pet').style.width).toBe('0%');
    expect(tela('painel').getByText('Gasto no mês').parentElement).toHaveTextContent(brl(-200));
  });

  it('sem lançamentos no mês não mostra "Últimos lançamentos"', async () => {
    await renderApp();
    expect(tela('painel').queryByText('Últimos lançamentos')).not.toBeInTheDocument();
  });

  it('últimos lançamentos: até 5 itens, crédito em verde com "+", e "Ver todos" leva para Lançamentos', async () => {
    for (let dia = 1; dia <= 6; dia++) lancamento({ data: `0${dia}/06/2026`, valor: 10, descricao: `Compra ${dia}`, categoria: 'Pet' });
    lancamento({ data: '09/06/2026', valor: -20, descricao: 'Crédito', categoria: 'Pet', observacao: 'estorno' });
    const { usuario } = await renderApp();
    const painel = tela('painel');

    expect(painel.getByText('7 no mês')).toBeInTheDocument();
    const itens = painel.getAllByTestId('lanc-item');
    expect(itens).toHaveLength(5);
    expect(itens[0]).toHaveTextContent(`Crédito`);
    expect(itens[0]).toHaveTextContent('Pet · estorno');
    expect(within(itens[0]!).getByText(`+${brl(20)}`).style.color).toBe('var(--green)');
    expect(itens[0]).toHaveTextContent('09/06/2026');
    expect(within(itens[0]!).queryByRole('button', { name: 'Excluir' })).not.toBeInTheDocument();

    await usuario.click(painel.getByRole('button', { name: 'Ver todos (7) →' }));
    expect(screen.getByTestId('screen-lancamentos')).toHaveClass('active');
  });

  it('item sem descrição mostra a categoria; categoria sem teto fica cinza', async () => {
    lancamento({ data: '01/06/2026', valor: 10, categoria: 'Sem teto' });
    await renderApp();
    const item = tela('painel').getByTestId('lanc-item');
    expect(item.querySelector('.lanc-desc')).toHaveTextContent('Sem teto');
    expect(item.querySelector('.lanc-icon')).toHaveTextContent('💳');
    expect(within(item).getByText(`-${brl(10)}`).style.color).toBe('var(--muted)');
  });
});
