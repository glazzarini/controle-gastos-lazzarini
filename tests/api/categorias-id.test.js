import { describe, it, expect, vi, beforeEach } from 'vitest';
import { makeSupabaseMock, mockRes } from '../helpers/supabaseMock.js';

beforeEach(() => {
  vi.resetModules();
  vi.doMock('ws', () => ({ default: {} }));
});

async function loadHandler(supabaseMock) {
  vi.doMock('@supabase/supabase-js', () => ({ createClient: () => supabaseMock }));
  const mod = await import('../../api/categorias/[id].js');
  return mod.default;
}

describe('api/categorias/[id].js', () => {
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
    it('atualiza teto sem mudar nome (não roda job de rename)', async () => {
      const supa = makeSupabaseMock([
        { data: { id: '1', nome: 'Pet', teto: '450.00' }, error: null }, // select atual
        { data: { id: '1', nome: 'Pet', teto: '500.00' }, error: null }, // update
      ]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { teto: 500 } }, res);
      expect(res.statusCode).toBe(200);
      expect(res.body).toEqual({
        updated: true,
        categoria: { id: '1', nome: 'Pet', teto: 500 },
        lancamentos_atualizados: 0,
      });
      expect(supa.from).toHaveBeenCalledTimes(2);
    });

    it('renomeia categoria e propaga para lançamentos históricos (job)', async () => {
      const supa = makeSupabaseMock([
        { data: { id: '1', nome: 'Pet', teto: '450.00' }, error: null }, // select atual
        { data: { id: '1', nome: 'Animais', teto: '450.00' }, error: null }, // update
        { data: [{ id: 'l1' }, { id: 'l2' }], error: null }, // update lancamentos
      ]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { nome: 'Animais' } }, res);
      expect(res.statusCode).toBe(200);
      expect(res.body).toEqual({
        updated: true,
        categoria: { id: '1', nome: 'Animais', teto: 450 },
        lancamentos_atualizados: 2,
      });
      expect(supa.from).toHaveBeenCalledTimes(3);
      expect(supa.from).toHaveBeenNthCalledWith(3, 'lancamentos');
    });

    it('não roda job quando o nome enviado é igual ao atual', async () => {
      const supa = makeSupabaseMock([
        { data: { id: '1', nome: 'Pet', teto: '450.00' }, error: null },
        { data: { id: '1', nome: 'Pet', teto: '450.00' }, error: null },
      ]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { nome: 'Pet' } }, res);
      expect(res.body.lancamentos_atualizados).toBe(0);
      expect(supa.from).toHaveBeenCalledTimes(2);
    });

    it('retorna 404 quando categoria não existe', async () => {
      const supa = makeSupabaseMock([{ data: null, error: { code: 'PGRST116', message: 'not found' } }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: 'x' }, body: { teto: 10 } }, res);
      expect(res.statusCode).toBe(404);
    });

    it('retorna 500 em erro inesperado ao buscar categoria atual', async () => {
      const supa = makeSupabaseMock([{ data: null, error: { code: 'XXXXX', message: 'db down' } }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: 'x' }, body: { teto: 10 } }, res);
      expect(res.statusCode).toBe(500);
      expect(res.body).toEqual({ error: 'db down' });
    });

    it('rejeita nome vazio (string em branco)', async () => {
      const supa = makeSupabaseMock([{ data: { id: '1', nome: 'Pet', teto: '450' }, error: null }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { nome: '   ' } }, res);
      expect(res.statusCode).toBe(400);
    });

    it('rejeita nome enviado como string vazia', async () => {
      const supa = makeSupabaseMock([{ data: { id: '1', nome: 'Pet', teto: '450' }, error: null }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { nome: '' } }, res);
      expect(res.statusCode).toBe(400);
    });

    it('rejeita teto inválido', async () => {
      const supa = makeSupabaseMock([{ data: { id: '1', nome: 'Pet', teto: '450' }, error: null }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { teto: 'abc' } }, res);
      expect(res.statusCode).toBe(400);
    });

    it('rejeita teto negativo', async () => {
      const supa = makeSupabaseMock([{ data: { id: '1', nome: 'Pet', teto: '450' }, error: null }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { teto: -1 } }, res);
      expect(res.statusCode).toBe(400);
    });

    it('retorna 409 quando o novo nome já existe (unique violation)', async () => {
      const supa = makeSupabaseMock([
        { data: { id: '1', nome: 'Pet', teto: '450' }, error: null },
        { data: null, error: { code: '23505', message: 'duplicate' } },
      ]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { nome: 'Guitarra' } }, res);
      expect(res.statusCode).toBe(409);
    });

    it('retorna 500 em outro erro ao atualizar', async () => {
      const supa = makeSupabaseMock([
        { data: { id: '1', nome: 'Pet', teto: '450' }, error: null },
        { data: null, error: { code: 'XXXXX', message: 'update falhou' } },
      ]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { teto: 10 } }, res);
      expect(res.statusCode).toBe(500);
      expect(res.body).toEqual({ error: 'update falhou' });
    });

    it('retorna 500 quando o job de rename falha', async () => {
      const supa = makeSupabaseMock([
        { data: { id: '1', nome: 'Pet', teto: '450' }, error: null },
        { data: { id: '1', nome: 'Animais', teto: '450' }, error: null },
        { data: null, error: new Error('job falhou') },
      ]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { nome: 'Animais' } }, res);
      expect(res.statusCode).toBe(500);
      expect(res.body).toEqual({ error: 'job falhou' });
    });
  });

  describe('DELETE', () => {
    it('exclui categoria sem lançamentos associados', async () => {
      const supa = makeSupabaseMock([
        { data: { nome: 'Pet' }, error: null },
        { count: 0, error: null },
        { error: null },
      ]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'DELETE', query: { id: '1' } }, res);
      expect(res.statusCode).toBe(200);
      expect(res.body).toEqual({ deleted: true });
    });

    it('bloqueia exclusão quando há lançamentos em uso', async () => {
      const supa = makeSupabaseMock([
        { data: { nome: 'Pet' }, error: null },
        { count: 3, error: null },
      ]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'DELETE', query: { id: '1' } }, res);
      expect(res.statusCode).toBe(409);
      expect(res.body.lancamentos_em_uso).toBe(3);
    });

    it('retorna 404 quando categoria não existe', async () => {
      const supa = makeSupabaseMock([{ data: null, error: { code: 'PGRST116', message: 'not found' } }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'DELETE', query: { id: 'x' } }, res);
      expect(res.statusCode).toBe(404);
    });

    it('retorna 500 em erro inesperado ao buscar categoria', async () => {
      const supa = makeSupabaseMock([{ data: null, error: { code: 'XXXXX', message: 'db down' } }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'DELETE', query: { id: 'x' } }, res);
      expect(res.statusCode).toBe(500);
    });

    it('retorna 500 quando a contagem de lançamentos falha', async () => {
      const supa = makeSupabaseMock([
        { data: { nome: 'Pet' }, error: null },
        { count: null, error: new Error('count falhou') },
      ]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'DELETE', query: { id: '1' } }, res);
      expect(res.statusCode).toBe(500);
    });

    it('retorna 500 quando o delete falha', async () => {
      const supa = makeSupabaseMock([
        { data: { nome: 'Pet' }, error: null },
        { count: 0, error: null },
        { error: new Error('delete falhou') },
      ]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'DELETE', query: { id: '1' } }, res);
      expect(res.statusCode).toBe(500);
    });
  });

  it('método não suportado retorna 405', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'PATCH', query: { id: '1' } }, res);
    expect(res.statusCode).toBe(405);
  });
});
