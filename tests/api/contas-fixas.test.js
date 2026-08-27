import { describe, it, expect, vi, beforeEach } from 'vitest';
import { makeSupabaseMock, mockRes } from '../helpers/supabaseMock.js';

beforeEach(() => {
  vi.resetModules();
  vi.doMock('ws', () => ({ default: {} }));
});

async function loadHandler(supabaseMock) {
  vi.doMock('@supabase/supabase-js', () => ({ createClient: () => supabaseMock }));
  const mod = await import('../../api/contas-fixas.js');
  return mod.default;
}

describe('api/contas-fixas.js', () => {
  it('OPTIONS retorna 200 e encerra', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'OPTIONS' }, res);
    expect(res.statusCode).toBe(200);
    expect(res.ended).toBe(true);
  });

  it('GET lista contas fixas com valor numérico', async () => {
    const supa = makeSupabaseMock([
      { data: [{ id: '1', nome: 'Condomínio', valor: '1600.00' }], error: null },
    ]);
    const handler = await loadHandler(supa);
    const res = mockRes();
    await handler({ method: 'GET' }, res);
    expect(res.statusCode).toBe(200);
    expect(res.body).toEqual([{ id: '1', nome: 'Condomínio', valor: 1600 }]);
  });

  it('GET retorna 500 em erro do supabase', async () => {
    const supa = makeSupabaseMock([{ data: null, error: new Error('boom') }]);
    const handler = await loadHandler(supa);
    const res = mockRes();
    await handler({ method: 'GET' }, res);
    expect(res.statusCode).toBe(500);
    expect(res.body).toEqual({ error: 'boom' });
  });

  it('POST cria conta fixa válida', async () => {
    const supa = makeSupabaseMock([
      { data: { id: '2', nome: 'Internet', valor: '120.00' }, error: null },
    ]);
    const handler = await loadHandler(supa);
    const res = mockRes();
    await handler({ method: 'POST', body: { nome: '  Internet  ', valor: 120 } }, res);
    expect(res.statusCode).toBe(201);
    expect(res.body).toEqual({ id: '2', nome: 'Internet', valor: 120 });
  });

  it('POST rejeita nome vazio (string em branco)', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'POST', body: { nome: '  ', valor: 10 } }, res);
    expect(res.statusCode).toBe(400);
  });

  it('POST rejeita nome ausente no body', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'POST', body: { valor: 10 } }, res);
    expect(res.statusCode).toBe(400);
  });

  it('POST rejeita valor inválido (NaN)', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'POST', body: { nome: 'X', valor: 'abc' } }, res);
    expect(res.statusCode).toBe(400);
  });

  it('POST rejeita valor negativo', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'POST', body: { nome: 'X', valor: -1 } }, res);
    expect(res.statusCode).toBe(400);
  });

  it('POST retorna 500 em erro do supabase', async () => {
    const supa = makeSupabaseMock([{ data: null, error: new Error('insert falhou') }]);
    const handler = await loadHandler(supa);
    const res = mockRes();
    await handler({ method: 'POST', body: { nome: 'X', valor: 10 } }, res);
    expect(res.statusCode).toBe(500);
    expect(res.body).toEqual({ error: 'insert falhou' });
  });

  it('método não suportado retorna 405', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'PATCH' }, res);
    expect(res.statusCode).toBe(405);
  });
});
