import { describe, it, expect, vi, beforeEach } from 'vitest';
import { makeSupabaseMock, mockRes } from '../helpers/supabaseMock.js';

beforeEach(() => {
  vi.resetModules();
  vi.doMock('ws', () => ({ default: {} }));
});

async function loadHandler(supabaseMock) {
  vi.doMock('@supabase/supabase-js', () => ({ createClient: () => supabaseMock }));
  const mod = await import('../../api/config.js');
  return mod.default;
}

describe('api/config.js', () => {
  it('OPTIONS retorna 200 e encerra', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'OPTIONS' }, res);
    expect(res.statusCode).toBe(200);
    expect(res.ended).toBe(true);
  });

  it('GET retorna config completa com todos os valores presentes', async () => {
    const supa = makeSupabaseMock([
      { data: [
        { chave: 'renda_guilherme', valor: '20000' },
        { chave: 'renda_esposa', valor: '14000' },
        { chave: 'meta_fatura', valor: '12000' },
      ], error: null },
      { data: [{ nome: 'Pet', teto: '450.00' }], error: null },
      { data: [{ id: 'cf1', nome: 'Condomínio', valor: '1600.00' }], error: null },
    ]);
    const handler = await loadHandler(supa);
    const res = mockRes();
    await handler({ method: 'GET' }, res);

    expect(res.statusCode).toBe(200);
    expect(res.body).toEqual({
      renda_guilherme: 20000,
      renda_esposa: 14000,
      meta_fatura: 12000,
      total_fixos: 1600,
      tetos: { Pet: 450 },
      contas_fixas: [{ id: 'cf1', nome: 'Condomínio', valor: 1600 }],
    });
  });

  it('GET aplica defaults quando config/contas_fixas estão vazias', async () => {
    const supa = makeSupabaseMock([
      { data: [], error: null },
      { data: [], error: null },
      { data: [], error: null },
    ]);
    const handler = await loadHandler(supa);
    const res = mockRes();
    await handler({ method: 'GET' }, res);

    expect(res.statusCode).toBe(200);
    expect(res.body).toEqual({
      renda_guilherme: 0,
      renda_esposa: 0,
      meta_fatura: 12000,
      total_fixos: 0,
      tetos: {},
      contas_fixas: [],
    });
  });

  it('GET retorna 500 quando a query de config falha', async () => {
    const supa = makeSupabaseMock([
      { data: null, error: new Error('falha config') },
      { data: [], error: null },
      { data: [], error: null },
    ]);
    const handler = await loadHandler(supa);
    const res = mockRes();
    await handler({ method: 'GET' }, res);
    expect(res.statusCode).toBe(500);
    expect(res.body).toEqual({ error: 'falha config' });
  });

  it('GET retorna 500 quando a query de categorias falha', async () => {
    const supa = makeSupabaseMock([
      { data: [], error: null },
      { data: null, error: new Error('falha categorias') },
      { data: [], error: null },
    ]);
    const handler = await loadHandler(supa);
    const res = mockRes();
    await handler({ method: 'GET' }, res);
    expect(res.statusCode).toBe(500);
    expect(res.body).toEqual({ error: 'falha categorias' });
  });

  it('GET retorna 500 quando a query de contas_fixas falha', async () => {
    const supa = makeSupabaseMock([
      { data: [], error: null },
      { data: [], error: null },
      { data: null, error: new Error('falha contas_fixas') },
    ]);
    const handler = await loadHandler(supa);
    const res = mockRes();
    await handler({ method: 'GET' }, res);
    expect(res.statusCode).toBe(500);
    expect(res.body).toEqual({ error: 'falha contas_fixas' });
  });

  it('PUT atualiza renda_guilherme e renda_esposa', async () => {
    const supa = makeSupabaseMock([{ error: null }]);
    const handler = await loadHandler(supa);
    const res = mockRes();
    await handler({ method: 'PUT', body: { renda_guilherme: 21000, renda_esposa: 15000 } }, res);
    expect(res.statusCode).toBe(200);
    expect(res.body).toEqual({ updated: true });
  });

  it('PUT atualiza só renda_guilherme', async () => {
    const supa = makeSupabaseMock([{ error: null }]);
    const handler = await loadHandler(supa);
    const res = mockRes();
    await handler({ method: 'PUT', body: { renda_guilherme: 21000 } }, res);
    expect(res.statusCode).toBe(200);
  });

  it('PUT atualiza só renda_esposa', async () => {
    const supa = makeSupabaseMock([{ error: null }]);
    const handler = await loadHandler(supa);
    const res = mockRes();
    await handler({ method: 'PUT', body: { renda_esposa: 15000 } }, res);
    expect(res.statusCode).toBe(200);
  });

  it('PUT rejeita renda_guilherme não numérico', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'PUT', body: { renda_guilherme: 'abc' } }, res);
    expect(res.statusCode).toBe(400);
  });

  it('PUT rejeita renda_guilherme negativo', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'PUT', body: { renda_guilherme: -5 } }, res);
    expect(res.statusCode).toBe(400);
  });

  it('PUT rejeita renda_esposa não numérico', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'PUT', body: { renda_esposa: 'abc' } }, res);
    expect(res.statusCode).toBe(400);
  });

  it('PUT rejeita renda_esposa negativo', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'PUT', body: { renda_esposa: -1 } }, res);
    expect(res.statusCode).toBe(400);
  });

  it('PUT sem nenhum campo retorna 400', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'PUT', body: {} }, res);
    expect(res.statusCode).toBe(400);
    expect(res.body.error).toMatch(/Informe/);
  });

  it('PUT retorna 500 em erro do supabase', async () => {
    const supa = makeSupabaseMock([{ error: new Error('upsert falhou') }]);
    const handler = await loadHandler(supa);
    const res = mockRes();
    await handler({ method: 'PUT', body: { renda_guilherme: 1000 } }, res);
    expect(res.statusCode).toBe(500);
    expect(res.body).toEqual({ error: 'upsert falhou' });
  });

  it('método não suportado retorna 405', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'DELETE' }, res);
    expect(res.statusCode).toBe(405);
  });
});
