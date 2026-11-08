import { fireEvent, screen, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { banco, falhar, requisicoes, servidor } from '../test/servidor';
import { brl, renderApp, tela, toast } from '../test/render';

async function abrirLancar() {
  const r = await renderApp();
  await r.usuario.click(screen.getByRole('button', { name: /^Lançar/ }));
  return { ...r, t: tela('lancar') };
}

const campo = (rotulo: string) => tela('lancar').getByLabelText(new RegExp(`^${rotulo}`));
const postado = () => requisicoes.filter(r => r.metodo === 'POST' && r.caminho === '/api/lancamentos').map(r => r.corpo);

describe('Lançar', () => {
  it('data padrão é hoje no horário local e categorias vêm da API em ordem alfabética', async () => {
    const { t } = await abrirLancar();
    expect(campo('Data')).toHaveValue('2026-06-15');
    const opcoes = t.getAllByRole('option').slice(0, 4).map(o => o.textContent);
    expect(opcoes).toEqual(['Selecione...', 'Alimentacao fora', 'Guitarra', 'Pet']);
    expect(t.queryByLabelText('Número de parcelas')).not.toBeInTheDocument();
  });

  it.each([
    ['', 'Pet', 'Informe um valor válido'],
    ['0', 'Pet', 'Informe um valor válido'],
    ['10', '', 'Selecione uma categoria'],
  ])('valida: valor "%s" categoria "%s" → %s', async (valor, cat, mensagem) => {
    const { usuario } = await abrirLancar();
    if (valor) await usuario.type(campo('Valor'), valor);
    if (cat) await usuario.selectOptions(campo('Categoria'), cat);
    await usuario.click(screen.getByRole('button', { name: 'Salvar lançamento' }));
    expect(toast()).toHaveTextContent(mensagem);
    expect(toast()).toHaveClass('error');
    expect(postado()).toHaveLength(0);
  });

  it('valida data vazia e parcelas fora de 2–48', async () => {
    const { usuario } = await abrirLancar();
    await usuario.type(campo('Valor'), '10');
    await usuario.selectOptions(campo('Categoria'), 'Pet');
    fireEvent.change(campo('Data'), { target: { value: '' } });
    await usuario.click(screen.getByRole('button', { name: 'Salvar lançamento' }));
    expect(toast()).toHaveTextContent('Informe a data');

    fireEvent.change(campo('Data'), { target: { value: '2026-06-10' } });
    await usuario.selectOptions(campo('Tipo'), 'Parcelado');
    await usuario.clear(campo('Número de parcelas'));
    await usuario.type(campo('Número de parcelas'), '49');
    await usuario.click(screen.getByRole('button', { name: 'Salvar lançamento' }));
    expect(toast()).toHaveTextContent('Parcelas entre 2 e 48');

    await usuario.clear(campo('Número de parcelas'));
    await usuario.type(campo('Número de parcelas'), '-3');
    await usuario.click(screen.getByRole('button', { name: 'Salvar lançamento' }));
    expect(toast()).toHaveTextContent('Parcelas entre 2 e 48');
    expect(postado()).toHaveLength(0);
  });

  it('salva à vista, mostra toast, limpa o formulário e atualiza o painel', async () => {
    const { usuario } = await abrirLancar();
    await usuario.type(campo('Valor'), '89.9');
    await usuario.selectOptions(campo('Categoria'), 'Pet');
    await usuario.type(campo('Descrição'), '  Ração  ');
    fireEvent.change(campo('Data'), { target: { value: '2026-06-03' } });
    await usuario.type(campo('Observação'), ' Petz ');
    await usuario.click(screen.getByRole('button', { name: 'Salvar lançamento' }));

    await waitFor(() => expect(toast()).toHaveTextContent('Lançamento salvo! ✓'));
    expect(postado()).toEqual([{ data: '03/06/2026', descricao: 'Ração', categoria: 'Pet', valor: 89.9, observacao: 'Petz', tipo: 'A vista', parcelas: 1 }]);
    expect(campo('Valor')).toHaveValue(null);
    expect(campo('Categoria')).toHaveValue('');
    expect(campo('Descrição')).toHaveValue('');
    expect(campo('Observação')).toHaveValue('');
    expect(campo('Data')).toHaveValue('2026-06-15');
    expect(tela('painel').getByText('Ração')).toBeInTheDocument();
  });

  it('crédito: aceita valor negativo', async () => {
    const { usuario } = await abrirLancar();
    await usuario.type(campo('Valor'), '-50');
    await usuario.selectOptions(campo('Categoria'), 'Pet');
    await usuario.click(screen.getByRole('button', { name: 'Salvar lançamento' }));
    await waitFor(() => expect(toast()).toHaveTextContent('Lançamento salvo! ✓'));
    expect(postado()[0]).toMatchObject({ valor: -50 });
    expect(banco.lancamentos[0]!.valor).toBe(-50);
  });

  it('parcelado: preview com última parcela ajustada ao fim do mês e envio das parcelas', async () => {
    const { usuario, t } = await abrirLancar();
    await usuario.type(campo('Valor'), '300');
    await usuario.selectOptions(campo('Categoria'), 'Pet');
    fireEvent.change(campo('Data'), { target: { value: '2026-01-31' } });
    await usuario.selectOptions(campo('Tipo'), 'Parcelado');

    expect(t.getByText(`2x de ${brl(150)} · vai até fev. de 26`)).toBeInTheDocument();
    await usuario.clear(campo('Número de parcelas'));
    await usuario.type(campo('Número de parcelas'), '3');
    expect(t.getByText(`3x de ${brl(100)} · vai até mar. de 26`)).toBeInTheDocument();

    await usuario.click(screen.getByRole('button', { name: 'Salvar lançamento' }));
    await waitFor(() => expect(toast()).toHaveTextContent('3 parcelas salvas! ✓'));
    expect(postado()[0]).toMatchObject({ parcelas: 3, tipo: 'Parcelado' });
    expect(banco.lancamentos.map(l => l.data)).toEqual(['31/01/2026', '28/02/2026', '31/03/2026']);
    expect(campo('Tipo')).toHaveValue('A vista');
  });

  it('preview some com menos de 2 parcelas ou sem data; valor vazio conta como 0', async () => {
    const { usuario, t } = await abrirLancar();
    await usuario.selectOptions(campo('Tipo'), 'Parcelado');
    expect(t.getByText(`2x de ${brl(0)} · vai até jul. de 26`)).toBeInTheDocument();
    await usuario.clear(campo('Número de parcelas'));
    await usuario.type(campo('Número de parcelas'), '1');
    expect(t.queryByText(/vai até/)).not.toBeInTheDocument();
    await usuario.clear(campo('Número de parcelas'));
    await usuario.type(campo('Número de parcelas'), '4');
    fireEvent.change(campo('Data'), { target: { value: '' } });
    expect(t.queryByText(/vai até/)).not.toBeInTheDocument();
  });

  it('parcelas vazio em Parcelado envia 1 parcela', async () => {
    const { usuario } = await abrirLancar();
    await usuario.type(campo('Valor'), '10');
    await usuario.selectOptions(campo('Categoria'), 'Pet');
    await usuario.selectOptions(campo('Tipo'), 'Parcelado');
    await usuario.clear(campo('Número de parcelas'));
    await usuario.click(screen.getByRole('button', { name: 'Salvar lançamento' }));
    await waitFor(() => expect(toast()).toHaveTextContent('Lançamento salvo! ✓'));
    expect(postado()[0]).toMatchObject({ parcelas: 1 });
  });

  it('mostra "Salvando..." durante o envio e o erro da API', async () => {
    servidor.use(http.post('/api/lancamentos', async () => {
      await new Promise(r => setTimeout(r, 30));
      return HttpResponse.json({ error: 'tipo inválido' }, { status: 500 });
    }, { once: true }));
    const { usuario } = await abrirLancar();
    await usuario.type(campo('Valor'), '10');
    await usuario.selectOptions(campo('Categoria'), 'Pet');
    await usuario.click(screen.getByRole('button', { name: 'Salvar lançamento' }));
    expect(screen.getByRole('button', { name: 'Salvando...' })).toBeDisabled();
    await waitFor(() => expect(toast()).toHaveTextContent('Erro ao salvar: tipo inválido'));
    expect(screen.getByRole('button', { name: 'Salvar lançamento' })).toBeEnabled();
    expect(campo('Valor')).toHaveValue(10);
  });

  it('erro genérico de rede', async () => {
    falhar('post', '/api/lancamentos', 500, 'db down');
    const { usuario } = await abrirLancar();
    await usuario.type(campo('Valor'), '10');
    await usuario.selectOptions(campo('Categoria'), 'Pet');
    await usuario.click(screen.getByRole('button', { name: 'Salvar lançamento' }));
    await waitFor(() => expect(toast()).toHaveTextContent('Erro ao salvar: db down'));
  });
});
