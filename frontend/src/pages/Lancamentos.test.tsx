import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { banco, falhar, lancamento, requisicoes, servidor } from '../test/servidor';
import { brl, renderApp, tela, toast } from '../test/render';

async function abrir(nome: RegExp) {
  const r = await renderApp();
  await r.usuario.click(screen.getByRole('button', { name: nome }));
  return r;
}
const abrirLancamentos = () => abrir(/^Lançamentos/);
const confirmarDialogo = () => screen.getByRole('button', { name: 'Confirmar' });
const dialogo = () => screen.getByRole('dialog');

describe('Lançamentos', () => {
  it('mês sem lançamentos mostra o vazio', async () => {
    await abrirLancamentos();
    expect(tela('lancamentos').getByText('Nenhum lançamento em jun/26')).toBeInTheDocument();
  });

  it('total lançado (singular/plural e cores) e chips das categorias usadas', async () => {
    lancamento({ data: '01/06/2026', valor: 10000, categoria: 'Pet' });
    await abrirLancamentos();
    const t = tela('lancamentos');
    expect(t.getByText(brl(10000))).toHaveClass('amber');
    expect(t.getByText('1 lançamento')).toBeInTheDocument();
  });

  it('total verde e vermelho', async () => {
    lancamento({ data: '01/06/2026', valor: 13000, categoria: 'Pet' });
    lancamento({ data: '01/07/2026', valor: 10, categoria: 'Pet' });
    const { usuario } = await abrirLancamentos();
    expect(tela('lancamentos').getByText(brl(13000))).toHaveClass('red');
    await usuario.click(screen.getByRole('button', { name: 'Selecionar mês' }));
    await usuario.click(within(screen.getByTestId('month-modal')).getByText('jul/26'));
    expect(tela('lancamentos').getByText(brl(10), { selector: '.metric-value' })).toHaveClass('green');
  });

  it('chips: Todos + categorias com teto usadas (ordem dos tetos) + categorias sem teto; filtro funciona', async () => {
    lancamento({ data: '01/06/2026', valor: 10, categoria: 'Pet', descricao: 'Ração' });
    lancamento({ data: '02/06/2026', valor: 20, categoria: 'Alimentacao fora', descricao: 'Almoço' });
    lancamento({ data: '03/06/2026', valor: 30, categoria: 'Antiga', descricao: 'Sem teto' });
    lancamento({ data: '04/06/2026', valor: 40, categoria: 'Antiga', descricao: 'Sem teto 2' });
    const { usuario } = await abrirLancamentos();
    const t = tela('lancamentos');

    expect(t.getByText('4 lançamentos')).toBeInTheDocument();
    const chips = t.getAllByRole('button').filter(b => b.classList.contains('filter-chip'));
    expect(chips.map(c => c.textContent)).toEqual(['Todos', '🍽 Alimentacao fora', '🐾 Pet', ' Antiga']);
    expect(chips[0]).toHaveClass('active');

    await usuario.click(chips[2]!);
    expect(t.getAllByTestId('lanc-item')).toHaveLength(1);
    expect(t.getByText('Ração')).toBeInTheDocument();
  });

  it('filtro sem resultados após excluir o último item da categoria', async () => {
    lancamento({ data: '01/06/2026', valor: 10, categoria: 'Pet', descricao: 'Ração' });
    lancamento({ data: '02/06/2026', valor: 20, categoria: 'Guitarra', descricao: 'Cordas' });
    const { usuario } = await abrirLancamentos();
    const t = tela('lancamentos');
    await usuario.click(t.getByRole('button', { name: /Pet/ }));

    await usuario.click(t.getByRole('button', { name: 'Excluir' }));
    expect(dialogo()).toHaveTextContent(`Excluir "Ração" de ${brl(10)} em 01/06/2026?`);
    await usuario.click(confirmarDialogo());

    await waitFor(() => expect(toast()).toHaveTextContent('Lançamento excluído ✓'));
    expect(t.getByText('Nenhum lançamento nesta categoria')).toBeInTheDocument();
    expect(screen.getByTestId('screen-lancamentos')).toHaveClass('active');
  });

  it('excluir pela lixeira não abre o detalhe; erro mostra toast', async () => {
    lancamento({ data: '01/06/2026', valor: 10, categoria: 'Pet' });
    falhar('delete', '/api/lancamentos/:id', 500, 'db down');
    const { usuario } = await abrirLancamentos();

    await usuario.click(tela('lancamentos').getByRole('button', { name: 'Excluir' }));
    expect(dialogo()).toHaveTextContent(`Excluir "Pet" de ${brl(10)}`);
    await usuario.click(confirmarDialogo());

    await waitFor(() => expect(toast()).toHaveTextContent('Erro ao excluir: db down'));
    expect(screen.getByTestId('screen-detalhe')).not.toHaveClass('active');
  });
});

describe('Detalhe do lançamento', () => {
  async function abrirDetalhe() {
    const r = await abrirLancamentos();
    await r.usuario.click(tela('lancamentos').getAllByTestId('lanc-item')[0]!);
    expect(screen.getByTestId('screen-detalhe')).toHaveClass('active');
    return { ...r, d: tela('detalhe') };
  }

  it('mostra os dados; a nav continua em Lançamentos; Voltar retorna', async () => {
    lancamento({ data: '05/06/2026', valor: 400, categoria: 'Pet', descricao: 'Vacina', observacao: 'Anual', tipo: 'Parcelado' });
    const { usuario, d } = await abrirDetalhe();

    expect(d.getByText('Vacina')).toBeInTheDocument();
    expect(d.getByText('🐾')).toBeInTheDocument();
    expect(d.getByText(`-${brl(400)}`).style.color).toBe('var(--amber)');
    expect(d.getByText('📅 Data').nextSibling).toHaveTextContent('05/06/2026');
    expect(d.getByText('💳 Tipo').nextSibling).toHaveTextContent('Parcelado');
    expect(d.getByText('📝 Observação').nextSibling).toHaveTextContent('Anual');
    expect(screen.getByRole('button', { name: /^Lançamentos/ })).toHaveClass('active');

    await usuario.click(d.getByRole('button', { name: 'Voltar' }));
    expect(screen.getByTestId('screen-lancamentos')).toHaveClass('active');
  });

  it('sem descrição e sem observação; crédito em verde', async () => {
    lancamento({ data: '05/06/2026', valor: -40, categoria: 'Pet' });
    const { d } = await abrirDetalhe();
    expect(d.getByText('(sem descrição)')).toBeInTheDocument();
    expect(d.queryByText('📝 Observação')).not.toBeInTheDocument();
    expect(d.getByText(`+${brl(40)}`).style.color).toBe('var(--green)');
  });

  it('excluir pelo detalhe volta para Lançamentos', async () => {
    lancamento({ data: '05/06/2026', valor: 10, categoria: 'Pet', descricao: 'Apagar' });
    const { usuario, d } = await abrirDetalhe();
    await usuario.click(d.getByRole('button', { name: 'Excluir lançamento' }));
    await usuario.click(confirmarDialogo());
    await waitFor(() => expect(screen.getByTestId('screen-lancamentos')).toHaveClass('active'));
    expect(toast()).toHaveTextContent('Lançamento excluído ✓');
    expect(banco.lancamentos).toHaveLength(0);
    expect(screen.getByTestId('screen-detalhe')).toBeEmptyDOMElement();
  });

  it('trocar o mês com o detalhe aberto mantém o detalhe; cor fica neutra (categoria sem gasto no novo mês)', async () => {
    lancamento({ data: '05/06/2026', valor: 400, categoria: 'Pet', descricao: 'Vacina' });
    const { usuario, d } = await abrirDetalhe();
    await usuario.click(screen.getByRole('button', { name: 'Selecionar mês' }));
    await usuario.click(within(screen.getByTestId('month-modal')).getByText('jul/26'));
    expect(screen.getByTestId('screen-detalhe')).toHaveClass('active');
    expect(d.getByText(`-${brl(400)}`).style.color).toBe('var(--muted)');
  });

  describe('modal de edição', () => {
    const modal = () => screen.getByTestId('edit-modal');
    const m = () => within(modal());

    it('abre preenchido e salva as alterações (PUT no contrato), atualizando o detalhe', async () => {
      lancamento({ data: '05/06/2026', valor: 400, categoria: 'Pet', descricao: 'Vacina', observacao: 'Anual' });
      const { usuario, d } = await abrirDetalhe();
      await usuario.click(d.getByRole('button', { name: 'Editar lançamento' }));

      expect(modal()).toHaveClass('open');
      expect(m().getByLabelText('Descrição')).toHaveValue('Vacina');
      expect(m().getByLabelText('Categoria')).toHaveValue('Pet');
      expect(m().getByLabelText('Valor')).toHaveValue(400);
      expect(m().getByLabelText('Data')).toHaveValue('2026-06-05');
      expect(m().getByLabelText('Tipo')).toHaveValue('A vista');
      expect(m().getByLabelText(/^Observação/)).toHaveValue('Anual');

      await usuario.clear(m().getByLabelText('Descrição'));
      await usuario.type(m().getByLabelText('Descrição'), ' Vacina V10 ');
      await usuario.selectOptions(m().getByLabelText('Categoria'), 'Guitarra');
      await usuario.clear(m().getByLabelText('Valor'));
      await usuario.type(m().getByLabelText('Valor'), '-15');
      fireEvent.change(m().getByLabelText('Data'), { target: { value: '2026-06-07' } });
      await usuario.selectOptions(m().getByLabelText('Tipo'), 'Parcelado');
      await usuario.clear(m().getByLabelText(/^Observação/));
      await usuario.click(m().getByRole('button', { name: 'Salvar alterações' }));

      await waitFor(() => expect(toast()).toHaveTextContent('Lançamento atualizado ✓'));
      expect(modal()).not.toHaveClass('open');
      expect(requisicoes.find(r => r.metodo === 'PUT')!.corpo).toEqual({
        data: '07/06/2026', descricao: 'Vacina V10', categoria: 'Guitarra', valor: -15, observacao: '', tipo: 'Parcelado',
      });
      expect(d.getByText('Vacina V10')).toBeInTheDocument();
      expect(d.getByText(`+${brl(15)}`)).toBeInTheDocument();
    });

    it('valor 0 → "Preencha todos os campos"; overlay fecha; clique dentro não fecha', async () => {
      lancamento({ data: '05/06/2026', valor: 400, categoria: 'Pet' });
      const { usuario, d } = await abrirDetalhe();
      await usuario.click(d.getByRole('button', { name: 'Editar lançamento' }));
      await usuario.clear(m().getByLabelText('Valor'));
      await usuario.type(m().getByLabelText('Valor'), '0');
      await usuario.click(m().getByRole('button', { name: 'Salvar alterações' }));
      expect(toast()).toHaveTextContent('Preencha todos os campos');

      await usuario.click(m().getByText('Editar lançamento'));
      expect(modal()).toHaveClass('open');
      fireEvent.click(modal());
      expect(modal()).not.toHaveClass('open');
    });

    it('salvar com o modal fechado não envia nada', async () => {
      lancamento({ data: '05/06/2026', valor: 400, categoria: 'Pet' });
      await abrirDetalhe();
      fireEvent.click(m().getByRole('button', { name: 'Salvar alterações', hidden: true }));
      expect(requisicoes.some(r => r.metodo === 'PUT')).toBe(false);
    });

    it('mostra "Salvando..." e o erro da API', async () => {
      lancamento({ data: '05/06/2026', valor: 400, categoria: 'Pet' });
      servidor.use(http.put('/api/lancamentos/:id', async () => {
        await new Promise(r => setTimeout(r, 30));
        return HttpResponse.json({ error: 'Lançamento não encontrado' }, { status: 404 });
      }, { once: true }));
      const { usuario, d } = await abrirDetalhe();
      await usuario.click(d.getByRole('button', { name: 'Editar lançamento' }));
      await usuario.click(m().getByRole('button', { name: 'Salvar alterações' }));
      expect(m().getByRole('button', { name: 'Salvando...' })).toBeDisabled();
      await waitFor(() => expect(toast()).toHaveTextContent('Erro ao salvar: Lançamento não encontrado'));
      expect(modal()).toHaveClass('open');
    });
  });
});
