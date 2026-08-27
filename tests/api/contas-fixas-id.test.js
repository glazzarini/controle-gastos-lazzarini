import { describe, it, expect, vi, beforeEach } from 'vitest';
import { makeSupabaseMock, mockRes } from '../helpers/supabaseMock.js';

beforeEach(() => {
  vi.resetModules();
  vi.doMock('ws', () => ({ default: {} }));
});

async function loadHandler(supabaseMock) {
  vi.doMock('@supabase/supabase-js', () => ({ createClient: () => supabaseMock }));
  const mod = await import('../../api/contas-fixas/[id].js');
  return mod.default;
}

describe('api/contas-fixas/[id].js', () => {
  it('OPTIONS retorna 200 e encerra', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'OPTIONS', query: {} }, res);
    expect(res.statusCode).toBe(200);
    expect(res.ended).toBe(true);
  });

  it('sem id retorna 400', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'PUT', query: {}, body: {} }, res);
    expect(res.statusCode).toBe(400);
  });

  describe('PUT', () => {
    it('atualiza nome e valor', async () => {
      const supa = makeSupabaseMock([
        { data: { id: '1', nome: 'Internet', valor: '150.00' }, error: null },
      ]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { nome: 'Internet', valor: 150 } }, res);
      expect(res.statusCode).toBe(200);
      expect(res.body).toEqual({ updated: true, conta_fixa: { id: '1', nome: 'Internet', valor: 150 } });
    });

    it('atualiza só o nome', async () => {
      const supa = makeSupabaseMock([{ data: { id: '1', nome: 'Novo Nome', valor: '100' }, error: null }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { nome: 'Novo Nome' } }, res);
      expect(res.statusCode).toBe(200);
    });

    it('atualiza só o valor', async () => {
      const supa = makeSupabaseMock([{ data: { id: '1', nome: 'X', valor: '200' }, error: null }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { valor: 200 } }, res);
      expect(res.statusCode).toBe(200);
    });

    it('rejeita nome vazio (string em branco)', async () => {
      const handler = await loadHandler(makeSupabaseMock([]));
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { nome: '   ' } }, res);
      expect(res.statusCode).toBe(400);
    });

    it('rejeita nome enviado como string vazia', async () => {
      const handler = await loadHandler(makeSupabaseMock([]));
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { nome: '' } }, res);
      expect(res.statusCode).toBe(400);
    });

    it('rejeita valor inválido', async () => {
      const handler = await loadHandler(makeSupabaseMock([]));
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { valor: 'abc' } }, res);
      expect(res.statusCode).toBe(400);
    });

    it('rejeita valor negativo', async () => {
      const handler = await loadHandler(makeSupabaseMock([]));
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { valor: -1 } }, res);
      expect(res.statusCode).toBe(400);
    });

    it('retorna 404 quando conta fixa não existe', async () => {
      const supa = makeSupabaseMock([{ data: null, error: { code: 'PGRST116', message: 'not found' } }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: 'x' }, body: { valor: 10 } }, res);
      expect(res.statusCode).toBe(404);
    });

    it('retorna 500 em outro erro do supabase', async () => {
      const supa = makeSupabaseMock([{ data: null, error: { code: 'XXXXX', message: 'update falhou' } }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: 'x' }, body: { valor: 10 } }, res);
      expect(res.statusCode).toBe(500);
      expect(res.body).toEqual({ error: 'update falhou' });
    });
  });

  describe('DELETE', () => {
    it('exclui conta fixa', async () => {
      const supa = makeSupabaseMock([{ error: null }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'DELETE', query: { id: '1' } }, res);
      expect(res.statusCode).toBe(200);
      expect(res.body).toEqual({ deleted: true });
    });

    it('retorna 500 em erro do supabase', async () => {
      const supa = makeSupabaseMock([{ error: new Error('delete falhou') }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'DELETE', query: { id: '1' } }, res);
      expect(res.statusCode).toBe(500);
      expect(res.body).toEqual({ error: 'delete falhou' });
    });
  });

  it('método não suportado retorna 405', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'PATCH', query: { id: '1' } }, res);
    expect(res.statusCode).toBe(405);
  });
});
