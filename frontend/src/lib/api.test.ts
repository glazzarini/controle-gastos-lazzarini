import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { api, ApiError, apiFetch } from './api';
import { requisicoes, servidor } from '../test/servidor';

describe('apiFetch', () => {
  it('devolve o corpo em caso de sucesso', async () => {
    servidor.use(http.get('/api/x', () => HttpResponse.json({ ok: 1 })));
    await expect(apiFetch('/api/x')).resolves.toEqual({ ok: 1 });
  });

  it('lança ApiError com a mensagem e o status de { error }', async () => {
    servidor.use(http.get('/api/x', () => HttpResponse.json({ error: 'nome é obrigatório' }, { status: 400 })));
    const erro = (await apiFetch('/api/x').catch((e: unknown) => e)) as ApiError;
    expect(erro).toBeInstanceOf(ApiError);
    expect(erro.message).toBe('nome é obrigatório');
    expect(erro.status).toBe(400);
    expect(erro.name).toBe('ApiError');
  });

  it.each([
    ['corpo não-JSON', () => new HttpResponse('Bad Gateway', { status: 502 })],
    ['JSON null', () => HttpResponse.json(null, { status: 500 })],
    ['error vazio', () => HttpResponse.json({ error: '' }, { status: 500 })],
    ['error não-string', () => HttpResponse.json({ error: 42 }, { status: 500 })],
  ])('usa mensagem genérica quando não há error (%s)', async (_, resposta) => {
    servidor.use(http.get('/api/x', resposta));
    await expect(apiFetch('/api/x')).rejects.toThrow('Erro na requisição');
  });
});

describe('api', () => {
  it('chama cada rota com método e corpo do contrato', async () => {
    await api.obterConfig();
    await api.atualizarRenda({ renda_guilherme: 1, renda_esposa: 2 });
    await api.listarLancamentos();
    const { ids } = await api.criarLancamento({ data: '01/06/2026', descricao: 'a', categoria: 'Pet', valor: -5, observacao: '', tipo: 'A vista', parcelas: 1 });
    await api.atualizarLancamento(ids[0]!, { data: '02/06/2026', descricao: 'b', categoria: 'Pet', valor: 1, observacao: '', tipo: 'A vista' });
    await api.excluirLancamento(ids[0]!);
    await api.listarCategorias();
    const cat = await api.criarCategoria({ nome: 'Nova', teto: 1 });
    await api.atualizarCategoria(cat.id, { nome: 'Nova2', teto: 2 });
    await api.excluirCategoria(cat.id);
    const cf = await api.criarContaFixa({ nome: 'Luz', valor: 3 });
    await api.atualizarContaFixa(cf.id, { nome: 'Luz', valor: 4 });
    await api.excluirContaFixa(cf.id);

    expect(requisicoes.map(r => `${r.metodo} ${r.caminho.replace(/\/[a-z]+-\d+$/, '/:id')}`)).toEqual([
      'GET /api/config', 'PUT /api/config',
      'GET /api/lancamentos', 'POST /api/lancamentos', 'PUT /api/lancamentos/:id', 'DELETE /api/lancamentos/:id',
      'GET /api/categorias', 'POST /api/categorias', 'PUT /api/categorias/:id', 'DELETE /api/categorias/:id',
      'POST /api/contas-fixas', 'PUT /api/contas-fixas/:id', 'DELETE /api/contas-fixas/:id',
    ]);
    expect(requisicoes[1]!.corpo).toEqual({ renda_guilherme: 1, renda_esposa: 2 });
    expect(requisicoes[3]!.corpo).toMatchObject({ valor: -5, parcelas: 1 });
  });
});
