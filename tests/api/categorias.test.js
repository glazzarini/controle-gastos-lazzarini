import { describe, it, expect, vi, beforeEach } from 'vitest';
import { makeSupabaseMock, mockRes } from '../helpers/supabaseMock.js';

beforeEach(() => {
  vi.resetModules();
  vi.doMock('ws', () => ({ default: {} }));
});

async function loadHandler(supabaseMock) {
  vi.doMock('@supabase/supabase-js', () => ({ createClient: () => supabaseMock }));
  const mod = await import('../../api/categorias.js');
  return mod.default;
}

describe('api/categorias.js', () => {
  it('OPTIONS retorna 200 e encerra', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'OPTIONS' }, res);
    expect(res.statusCode).toBe(200);
    expect(res.ended).toBe(true);
  });

  it('GET lista categorias ordenadas por nome, com teto numérico', async () => {
    const supa = makeSupabaseMock([
      { data: [{ id: '1', nome: 'Pet', teto: '450.00' }], error: null },
    ]);
    const handler = await loadHandler(supa);
    const res = mockRes();
    await handler({ method: 'GET' }, res);
    expect(res.statusCode).toBe(200);
    expect(res.body).toEqual([{ id: '1', nome: 'Pet', teto: 450 }]);
    expect(supa.from).toHaveBeenCalledWith('categorias');
  });

  it('GET retorna 500 em erro do supabase', async () => {
    const supa = makeSupabaseMock([{ data: null, error: new Error('boom') }]);
    const handler = await loadHandler(supa);
    const res = mockRes();
    await handler({ method: 'GET' }, res);
    expect(res.statusCode).toBe(500);
    expect(res.body).toEqual({ error: 'boom' });
  });

  it('POST cria categoria válida', async () => {
    const supa = makeSupabaseMock([
      { data: { id: '2', nome: 'Nova Cat', teto: '100.00' }, error: null },
    ]);
    const handler = await loadHandler(supa);
    const res = mockRes();
    await handler({ method: 'POST', body: { nome: '  Nova Cat  ', teto: 100 } }, res);
    expect(res.statusCode).toBe(201);
    expect(res.body).toEqual({ id: '2', nome: 'Nova Cat', teto: 100 });
  });

  it('POST com teto omitido usa 0', async () => {
    const supa = makeSupabaseMock([
      { data: { id: '3', nome: 'Sem Teto', teto: '0' }, error: null },
    ]);
    const handler = await loadHandler(supa);
    const res = mockRes();
    await handler({ method: 'POST', body: { nome: 'Sem Teto' } }, res);
    expect(res.statusCode).toBe(201);
    expect(res.body.teto).toBe(0);
  });

  it('POST rejeita nome vazio (string em branco)', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'POST', body: { nome: '   ', teto: 10 } }, res);
    expect(res.statusCode).toBe(400);
  });

  it('POST rejeita nome ausente no body', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'POST', body: { teto: 10 } }, res);
    expect(res.statusCode).toBe(400);
  });

  it('POST rejeita teto inválido (NaN)', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'POST', body: { nome: 'X', teto: 'abc' } }, res);
    expect(res.statusCode).toBe(400);
  });

  it('POST rejeita teto negativo', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'POST', body: { nome: 'X', teto: -1 } }, res);
    expect(res.statusCode).toBe(400);
  });

  it('POST retorna 409 quando nome já existe (unique violation)', async () => {
    const supa = makeSupabaseMock([{ data: null, error: { code: '23505', message: 'duplicate' } }]);
    const handler = await loadHandler(supa);
    const res = mockRes();
    await handler({ method: 'POST', body: { nome: 'Repetida', teto: 10 } }, res);
    expect(res.statusCode).toBe(409);
  });

  it('POST retorna 500 em outro erro do supabase', async () => {
    const supa = makeSupabaseMock([{ data: null, error: { code: 'XXXXX', message: 'outro erro' } }]);
    const handler = await loadHandler(supa);
    const res = mockRes();
    await handler({ method: 'POST', body: { nome: 'X', teto: 10 } }, res);
    expect(res.statusCode).toBe(500);
    expect(res.body).toEqual({ error: 'outro erro' });
  });

  it('método não suportado retorna 405', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'PATCH' }, res);
    expect(res.statusCode).toBe(405);
  });
});
