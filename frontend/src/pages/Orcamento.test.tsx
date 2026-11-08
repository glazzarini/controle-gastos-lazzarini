import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { banco, falhar, lancamento, requisicoes, servidor } from '../test/servidor';
import { brl, renderApp, tela, toast } from '../test/render';

async function abrirOrcamento() {
  const r = await renderApp();
  await r.usuario.click(screen.getByRole('button', { name: /^Orçamento/ }));
  return { ...r, o: tela('orcamento') };
}
const modal = () => screen.getByTestId('form-modal');
const m = () => within(modal());
const card = (titulo: string) => tela('orcamento').getByText(titulo).closest('.card') as HTMLElement;
const linha = (texto: string) => tela('orcamento').getByText(texto, { exact: false }).closest('.orc-row') as HTMLElement;
const corpos = (metodo: string, caminho: RegExp) => requisicoes.filter(r => r.metodo === metodo && caminho.test(r.caminho)).map(r => r.corpo);

describe('Orçamento — métricas e resultado', () => {
  it('sobra, % poupança (verde), meta de 20%, renda e total fixos', async () => {
    lancamento({ data: '01/06/2026', valor: 1000, categoria: 'Pet' });
    const { o } = await abrirOrcamento();
    // renda 34.000 − fixos 1.724 − fatura 1.000 = 31.276 (92%)
    const sobra = o.getByText('Sobra / Poupança', { selector: '.metric-label' }).parentElement!;
    expect(within(sobra).getByText(brl(31276))).toHaveClass('green');
    expect(sobra).toHaveTextContent('92.0% da renda');
    expect(o.getByText('Meta poupança').parentElement).toHaveTextContent(brl(6800));
    expect(linha('Renda total')).toHaveTextContent(brl(34000));
    expect(linha('Total fixos')).toHaveTextContent(brl(1724));
    expect(linha('Fatura cartão').querySelector('.orc-value')!.getAttribute('style')).toContain('var(--green)');
    expect(card('Resultado jun/26')).toHaveTextContent(`🎯 Meta fatura${brl(12000)}`);
    expect(within(card('Resultado jun/26')).getByText('92.0%')).toHaveClass('green');
  });

  it('sobra negativa vermelha, poupança amarela/vermelha, fatura amarela/vermelha', async () => {
    banco.config = { renda_guilherme: 10000, renda_esposa: 0, meta_fatura: 12000 };
    banco.contasFixas = [{ id: 'cf-x', nome: 'Aluguel', valor: 1000, created_at: null }];
    lancamento({ data: '01/06/2026', valor: 7500, categoria: 'Pet' }); // sobra 1.500 (15%), fatura 62%
    lancamento({ data: '01/07/2026', valor: 9500, categoria: 'Pet' }); // sobra -500, fatura 79%
    lancamento({ data: '01/08/2026', valor: 12500, categoria: 'Pet' }); // fatura 104%
    const { usuario, o } = await abrirOrcamento();
    const trocarMes = async (label: string) => {
      await usuario.click(screen.getByRole('button', { name: 'Selecionar mês' }));
      await usuario.click(within(screen.getByTestId('month-modal')).getByText(label));
    };
    const corFatura = () => linha('Fatura cartão').querySelector('.orc-value')!.getAttribute('style');

    expect(within(card('Resultado jun/26')).getByText('15.0%')).toHaveClass('amber');
    expect(corFatura()).toContain('var(--green)');

    await trocarMes('jul/26');
    expect(o.getByText(brl(-500), { selector: '.metric-value' })).toHaveClass('red');
    expect(within(card('Resultado jul/26')).getByText('-5.0%')).toHaveClass('red');
    expect(corFatura()).toContain('var(--amber)');

    await trocarMes('ago/26');
    expect(corFatura()).toContain('var(--red)');
  });

  it('renda 0 é exibida como 0 (o index.html mostrava 20.000)', async () => {
    banco.config.renda_guilherme = 0;
    const { o } = await abrirOrcamento();
    expect(linha('Guilherme (PJ líquido)')).toHaveTextContent(brl(0));
    expect(o.getByText('Sobra / Poupança', { selector: '.metric-label' }).parentElement).toHaveTextContent('% da renda');
  });
});

describe('Orçamento — renda', () => {
  it('edita a renda', async () => {
    const { usuario, o } = await abrirOrcamento();
    await usuario.click(o.getByRole('button', { name: 'Editar renda' }));
    expect(modal()).toHaveClass('open');
    expect(m().getByText('Editar renda')).toBeInTheDocument();
    expect(m().getByLabelText('Guilherme (PJ líquido)')).toHaveValue(20000);
    expect(m().getByLabelText('Esposa (PJ líquido)')).toHaveValue(14000);

    await usuario.clear(m().getByLabelText('Guilherme (PJ líquido)'));
    await usuario.type(m().getByLabelText('Guilherme (PJ líquido)'), '21000.5');
    await usuario.click(m().getByRole('button', { name: 'Salvar' }));

    await waitFor(() => expect(toast()).toHaveTextContent('Renda atualizada ✓'));
    expect(modal()).not.toHaveClass('open');
    expect(corpos('PUT', /^\/api\/config$/)).toEqual([{ renda_guilherme: 21000.5, renda_esposa: 14000 }]);
    expect(linha('Guilherme (PJ líquido)')).toHaveTextContent(brl(21000.5));
  });

  it('rejeita valores inválidos e mantém o modal aberto', async () => {
    const { usuario, o } = await abrirOrcamento();
    await usuario.click(o.getByRole('button', { name: 'Editar renda' }));
    await usuario.clear(m().getByLabelText('Esposa (PJ líquido)'));
    await usuario.click(m().getByRole('button', { name: 'Salvar' }));
    expect(toast()).toHaveTextContent('Erro: Informe valores válidos');
    expect(modal()).toHaveClass('open');

    await usuario.type(m().getByLabelText('Esposa (PJ líquido)'), '-1');
    await usuario.click(m().getByRole('button', { name: 'Salvar' }));
    expect(toast()).toHaveTextContent('Erro: Informe valores válidos');

    await usuario.clear(m().getByLabelText('Esposa (PJ líquido)'));
    await usuario.type(m().getByLabelText('Esposa (PJ líquido)'), '1');
    await usuario.clear(m().getByLabelText('Guilherme (PJ líquido)'));
    await usuario.type(m().getByLabelText('Guilherme (PJ líquido)'), '-1');
    await usuario.click(m().getByRole('button', { name: 'Salvar' }));
    expect(toast()).toHaveTextContent('Erro: Informe valores válidos');
    expect(corpos('PUT', /^\/api\/config$/)).toHaveLength(0);
  });

  it('mostra "Salvando..."; overlay fecha o modal, clique dentro não', async () => {
    servidor.use(http.put('/api/config', async () => {
      await new Promise(r => setTimeout(r, 30));
      return HttpResponse.json({ updated: true });
    }, { once: true }));
    const { usuario, o } = await abrirOrcamento();
    await usuario.click(o.getByRole('button', { name: 'Editar renda' }));
    await usuario.click(m().getByRole('button', { name: 'Salvar' }));
    expect(m().getByRole('button', { name: 'Salvando...' })).toBeDisabled();
    await waitFor(() => expect(modal()).not.toHaveClass('open'));

    await usuario.click(o.getByRole('button', { name: 'Editar renda' }));
    await usuario.click(m().getByText('Editar renda'));
    expect(modal()).toHaveClass('open');
    fireEvent.click(modal());
    expect(modal()).not.toHaveClass('open');
  });
});

describe('Orçamento — contas fixas', () => {
  it('lista com ícone padrão, cria, edita e exclui', async () => {
    const { usuario, o } = await abrirOrcamento();
    expect(linha('Condomínio')).toHaveTextContent(`💰 Condomínio${brl(1600)}`);

    await usuario.click(o.getByRole('button', { name: '+ Adicionar conta fixa' }));
    expect(m().getByText('Nova conta fixa')).toBeInTheDocument();
    expect(m().getByLabelText('Nome')).toHaveValue('');
    await usuario.type(m().getByLabelText('Nome'), '  Internet ');
    await usuario.type(m().getByLabelText('Valor'), '120');
    await usuario.click(m().getByRole('button', { name: 'Salvar' }));
    await waitFor(() => expect(toast()).toHaveTextContent('Conta fixa criada ✓'));
    expect(corpos('POST', /contas-fixas/)).toEqual([{ nome: 'Internet', valor: 120 }]);
    expect(linha('Total fixos')).toHaveTextContent(brl(1844));

    await usuario.click(o.getByRole('button', { name: 'Editar Vivo' }));
    expect(m().getByText('Editar conta fixa')).toBeInTheDocument();
    expect(m().getByLabelText('Nome')).toHaveValue('Vivo');
    expect(m().getByLabelText('Valor')).toHaveValue(124);
    await usuario.clear(m().getByLabelText('Valor'));
    await usuario.type(m().getByLabelText('Valor'), '130');
    await usuario.click(m().getByRole('button', { name: 'Salvar' }));
    await waitFor(() => expect(toast()).toHaveTextContent('Conta fixa atualizada ✓'));
    expect(corpos('PUT', /contas-fixas\/cf-2/)).toEqual([{ nome: 'Vivo', valor: 130 }]);

    await usuario.click(o.getByRole('button', { name: 'Excluir Condomínio' }));
    expect(screen.getByRole('dialog')).toHaveTextContent('Excluir conta fixaExcluir "Condomínio"?');
    await usuario.click(screen.getByRole('button', { name: 'Confirmar' }));
    await waitFor(() => expect(toast()).toHaveTextContent('Conta fixa excluída ✓'));
    expect(o.queryByText('Condomínio', { exact: false })).not.toBeInTheDocument();
  });

  it('validações e erro ao excluir', async () => {
    falhar('delete', '/api/contas-fixas/:id', 500, 'db down');
    const { usuario, o } = await abrirOrcamento();
    await usuario.click(o.getByRole('button', { name: '+ Adicionar conta fixa' }));
    await usuario.click(m().getByRole('button', { name: 'Salvar' }));
    expect(toast()).toHaveTextContent('Erro: Informe o nome');
    await usuario.type(m().getByLabelText('Nome'), 'X');
    await usuario.click(m().getByRole('button', { name: 'Salvar' }));
    expect(toast()).toHaveTextContent('Erro: Valor inválido');
    await usuario.type(m().getByLabelText('Valor'), '-5');
    await usuario.click(m().getByRole('button', { name: 'Salvar' }));
    expect(toast()).toHaveTextContent('Erro: Valor inválido');
    fireEvent.click(modal());

    await usuario.click(o.getByRole('button', { name: 'Excluir Vivo' }));
    await usuario.click(screen.getByRole('button', { name: 'Confirmar' }));
    await waitFor(() => expect(toast()).toHaveTextContent('Erro ao excluir: db down'));
  });

  it('contas fixas padrão (API sem contas) aparecem com ícone e sem botões', async () => {
    banco.contasFixas = [];
    const { o } = await abrirOrcamento();
    expect(linha('Financiamento imóvel')).toHaveTextContent(`🏠 Financiamento imóvel${brl(7500)}`);
    expect(within(linha('Financiamento imóvel')).queryByRole('button')).not.toBeInTheDocument();
    expect(card('Contas fixas')).toHaveTextContent(`Total fixos${brl(0)}`);
    expect(o.getAllByTestId('conta-fixa')).toHaveLength(8);
  });
});

describe('Orçamento — categorias e tetos', () => {
  it('lista, cria, edita teto e renomeia (com propagação)', async () => {
    lancamento({ data: '01/06/2026', valor: 10, categoria: 'Pet' });
    lancamento({ data: '01/07/2026', valor: 10, categoria: 'Pet' });
    const { usuario, o } = await abrirOrcamento();
    expect(o.getAllByTestId('categoria').map(e => e.textContent!.replace(/\u00a0/g, ' '))).toEqual([
      `🍽 Alimentacao fora${brl(2000)}`, `🎸 Guitarra${brl(300)}`, `🐾 Pet${brl(450)}`,
    ]);

    await usuario.click(o.getByRole('button', { name: '+ Adicionar categoria' }));
    expect(m().getByText('Nova categoria')).toBeInTheDocument();
    await usuario.type(m().getByLabelText('Nome'), 'Academia');
    await usuario.type(m().getByLabelText('Teto mensal'), '200');
    await usuario.click(m().getByRole('button', { name: 'Salvar' }));
    await waitFor(() => expect(toast()).toHaveTextContent('Categoria criada ✓'));
    expect(o.getByText('Academia', { exact: false })).toBeInTheDocument();

    await usuario.click(o.getByRole('button', { name: 'Editar Guitarra' }));
    expect(m().getByText('Editar categoria')).toBeInTheDocument();
    expect(m().getByLabelText('Teto mensal')).toHaveValue(300);
    await usuario.clear(m().getByLabelText('Teto mensal'));
    await usuario.type(m().getByLabelText('Teto mensal'), '350');
    await usuario.click(m().getByRole('button', { name: 'Salvar' }));
    await waitFor(() => expect(toast()).toHaveTextContent('Categoria atualizada ✓'));

    await usuario.click(o.getByRole('button', { name: 'Editar Pet' }));
    await usuario.clear(m().getByLabelText('Nome'));
    await usuario.type(m().getByLabelText('Nome'), 'Animais');
    await usuario.click(m().getByRole('button', { name: 'Salvar' }));
    await waitFor(() => expect(toast()).toHaveTextContent('Categoria renomeada — 2 lançamento(s) do histórico atualizado(s) ✓'));
    expect(banco.lancamentos.every(l => l.categoria === 'Animais')).toBe(true);
    expect(o.getByText('Animais', { exact: false })).toBeInTheDocument();
  });

  it('validações; 409 ao criar e ao excluir em uso; exclusão ok', async () => {
    lancamento({ data: '01/06/2026', valor: 10, categoria: 'Pet' });
    const { usuario, o } = await abrirOrcamento();

    await usuario.click(o.getByRole('button', { name: '+ Adicionar categoria' }));
    await usuario.click(m().getByRole('button', { name: 'Salvar' }));
    expect(toast()).toHaveTextContent('Erro: Informe o nome');
    await usuario.type(m().getByLabelText('Nome'), 'Pet');
    await usuario.click(m().getByRole('button', { name: 'Salvar' }));
    expect(toast()).toHaveTextContent('Erro: Teto inválido');
    await usuario.type(m().getByLabelText('Teto mensal'), '-1');
    await usuario.click(m().getByRole('button', { name: 'Salvar' }));
    expect(toast()).toHaveTextContent('Erro: Teto inválido');
    await usuario.clear(m().getByLabelText('Teto mensal'));
    await usuario.type(m().getByLabelText('Teto mensal'), '1');
    await usuario.click(m().getByRole('button', { name: 'Salvar' }));
    await waitFor(() => expect(toast()).toHaveTextContent('Erro: Já existe uma categoria com esse nome'));
    fireEvent.click(modal());

    await usuario.click(o.getByRole('button', { name: 'Excluir Pet' }));
    expect(screen.getByRole('dialog')).toHaveTextContent('Excluir categoriaExcluir "Pet"?');
    await usuario.click(screen.getByRole('button', { name: 'Confirmar' }));
    await waitFor(() => expect(toast()).toHaveTextContent('Não é possível excluir: 1 lançamento(s) usam essa categoria'));
    expect(toast()).toHaveClass('error');

    await usuario.click(o.getByRole('button', { name: 'Excluir Guitarra' }));
    await usuario.click(screen.getByRole('button', { name: 'Confirmar' }));
    await waitFor(() => expect(toast()).toHaveTextContent('Categoria excluída ✓'));
    expect(o.queryByText('Guitarra', { exact: false })).not.toBeInTheDocument();
  });
});

describe('FormModal sem configuração', () => {
  it('salvar sem configuração não faz nada', async () => {
    const { FormModal } = await import('../components/FormModal');
    const { UiProvider } = await import('../state/Ui');
    render(<UiProvider><FormModal config={null} onFechar={() => {}} /></UiProvider>);
    fireEvent.click(screen.getByRole('button', { name: 'Salvar' }));
    expect(screen.getByTestId('form-modal')).not.toHaveClass('open');
    expect(within(screen.getByTestId('form-modal')).getByText('Editar')).toBeInTheDocument();
  });
});
