import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { App } from './App';
import { AppDataProvider } from './state/AppData';
import { UiProvider } from './state/Ui';
import { banco, lancamento, servidor } from './test/servidor';
import { brl, renderApp, tela, toast } from './test/render';

describe('App — navegação, mês e atualização', () => {
  it('abre no mês corrente, na tela Painel', async () => {
    await renderApp();
    expect(screen.getByRole('button', { name: 'Selecionar mês' })).toHaveTextContent('jun/26');
    expect(screen.getByTestId('screen-painel')).toHaveClass('screen', 'active');
    expect(screen.getByRole('button', { name: /Painel/ })).toHaveClass('active');
  });

  it('mostra "Carregando..." em todas as telas até a primeira resposta', async () => {
    servidor.use(http.get('/api/config', async () => { await new Promise(r => setTimeout(r, 30)); return HttpResponse.json(null, { status: 500 }); }, { once: true }));
    render(<UiProvider><AppDataProvider><App /></AppDataProvider></UiProvider>);
    for (const t of ['painel', 'lancamentos', 'orcamento', 'historico'] as const) {
      expect(tela(t).getByText('Carregando...')).toBeInTheDocument();
    }
    await tela('painel').findByText('Por categoria');
  });

  it('bottom nav troca de tela e marca o botão ativo', async () => {
    const { usuario } = await renderApp();
    for (const [rotulo, id] of [['Lançar', 'lancar'], ['Lançamentos', 'lancamentos'], ['Orçamento', 'orcamento'], ['Histórico', 'historico'], ['Painel', 'painel']] as const) {
      await usuario.click(screen.getByRole('button', { name: new RegExp(`^${rotulo}`) }));
      expect(screen.getByTestId(`screen-${id}`)).toHaveClass('active');
      expect(screen.getByRole('button', { name: new RegExp(`^${rotulo}`) })).toHaveClass('active');
    }
    expect(window.scrollTo).toHaveBeenCalledWith(0, 0);
  });

  it('seletor de mês: lista jun/26 a dez/28, troca o mês e fecha', async () => {
    lancamento({ data: '05/11/2026', valor: 99, descricao: 'Compra de novembro' });
    const { usuario } = await renderApp();
    const modal = screen.getByTestId('month-modal');

    await usuario.click(screen.getByRole('button', { name: 'Selecionar mês' }));
    expect(modal).toHaveClass('open');
    const opcoes = within(modal).getAllByRole('button');
    expect(opcoes).toHaveLength(31);
    expect(opcoes[0]).toHaveClass('active');
    expect(opcoes.at(-1)).toHaveTextContent('dez/28');

    await usuario.click(within(modal).getByText('nov/26'));
    expect(modal).not.toHaveClass('open');
    expect(screen.getByRole('button', { name: 'Selecionar mês' })).toHaveTextContent('nov/26');
    expect(tela('painel').getByText('Compra de novembro')).toBeInTheDocument();
  });

  it('seletor de mês fecha só ao clicar fora da folha', async () => {
    const { usuario } = await renderApp();
    const modal = screen.getByTestId('month-modal');
    await usuario.click(screen.getByRole('button', { name: 'Selecionar mês' }));
    await usuario.click(within(modal).getByText('Selecionar mês'));
    expect(modal).toHaveClass('open');
    fireEvent.click(modal);
    expect(modal).not.toHaveClass('open');
  });

  it('trocar o mês na tela Lançamentos volta o filtro para "Todos"', async () => {
    lancamento({ data: '05/06/2026', valor: 10, categoria: 'Pet' });
    lancamento({ data: '06/06/2026', valor: 10, categoria: 'Guitarra' });
    lancamento({ data: '05/07/2026', valor: 10, categoria: 'Pet' });
    const { usuario } = await renderApp();
    await usuario.click(screen.getByRole('button', { name: /^Lançamentos/ }));
    await usuario.click(tela('lancamentos').getByRole('button', { name: /Guitarra/ }));
    expect(tela('lancamentos').getByRole('button', { name: /Guitarra/ })).toHaveClass('active');

    await usuario.click(screen.getByRole('button', { name: 'Selecionar mês' }));
    await usuario.click(within(screen.getByTestId('month-modal')).getByText('jul/26'));

    expect(tela('lancamentos').getByRole('button', { name: 'Todos' })).toHaveClass('active');
  });

  it('trocar o mês em outra tela mantém o filtro', async () => {
    lancamento({ data: '05/06/2026', valor: 10, categoria: 'Pet' });
    lancamento({ data: '06/06/2026', valor: 10, categoria: 'Guitarra' });
    const { usuario } = await renderApp();
    await usuario.click(screen.getByRole('button', { name: /^Lançamentos/ }));
    await usuario.click(tela('lancamentos').getByRole('button', { name: /Guitarra/ }));
    await usuario.click(screen.getByRole('button', { name: /^Painel/ }));
    await usuario.click(screen.getByRole('button', { name: 'Selecionar mês' }));
    await usuario.click(within(screen.getByTestId('month-modal')).getByText('jun/26'));
    expect(tela('lancamentos').getByRole('button', { name: /Guitarra/ })).toHaveClass('active');
  });

  it('botão atualizar recarrega os dados e mostra toast', async () => {
    const { usuario } = await renderApp();
    lancamento({ data: '07/06/2026', valor: 42, descricao: 'Novo no servidor' });
    servidor.use(http.get('/api/lancamentos', async () => {
      await new Promise(r => setTimeout(r, 30));
      return HttpResponse.json(banco.lancamentos);
    }, { once: true }));

    const botao = screen.getByRole('button', { name: 'Atualizar dados' });
    await usuario.click(botao);
    expect(botao).toBeDisabled();
    expect(botao).toHaveClass('spinning');

    await waitFor(() => expect(toast()).toHaveTextContent('Atualizado ✓'));
    expect(botao).not.toBeDisabled();
    expect(tela('painel').getByText('Novo no servidor')).toBeInTheDocument();
    expect(tela('painel').getByText(`-${brl(42)}`)).toBeInTheDocument();
  });
});
