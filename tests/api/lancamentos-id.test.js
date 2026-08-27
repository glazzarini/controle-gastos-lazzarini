import { describe, it, expect, vi, beforeEach } from 'vitest';
import { makeSupabaseMock, mockRes } from '../helpers/supabaseMock.js';

beforeEach(() => {
  vi.resetModules();
  vi.doMock('ws', () => ({ default: {} }));
});

async function loadHandler(supabaseMock) {
  vi.doMock('@supabase/supabase-js', () => ({ createClient: () => supabaseMock }));
  const mod = await import('../../api/lancamentos/[id].js');
  return mod.default;
}

describe('api/lancamentos/[id].js', () => {
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
    await handler({ method: 'DELETE', query: {} }, res);
    expect(res.statusCode).toBe(400);
  });

  describe('DELETE', () => {
    it('exclui lançamento', async () => {
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
    });
  });

  describe('PUT', () => {
    it('atualiza todos os campos, convertendo data BR -> ISO e de volta', async () => {
      const supa = makeSupabaseMock([
        { data: { id: '1', data: '2026-06-05', descricao: 'Nova', categoria: 'Pet', valor: '99.00', observacao: 'obs', tipo: 'A vista' }, error: null },
      ]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({
        method: 'PUT',
        query: { id: '1' },
        body: { data: '05/06/2026', descricao: 'Nova', categoria: 'Pet', valor: 99, observacao: 'obs', tipo: 'A vista' },
      }, res);
      expect(res.statusCode).toBe(200);
      expect(res.body.lancamento.data).toBe('05/06/2026');
    });

    it('aceita data já em formato ISO (contém "-")', async () => {
      const supa = makeSupabaseMock([
        { data: { id: '1', data: '2026-06-05', categoria: 'Pet', valor: '10' }, error: null },
      ]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { data: '2026-06-05' } }, res);
      expect(res.statusCode).toBe(200);
    });

    it('atualiza apenas campos parciais (sem data)', async () => {
      const supa = makeSupabaseMock([
        { data: { id: '1', data: '2026-06-05', categoria: 'Pet', valor: '10' }, error: null },
      ]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { valor: 10 } }, res);
      expect(res.statusCode).toBe(200);
    });

    it('retorna 500 em erro do supabase', async () => {
      const supa = makeSupabaseMock([{ data: null, error: new Error('update falhou') }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { valor: 10 } }, res);
      expect(res.statusCode).toBe(500);
      expect(res.body).toEqual({ error: 'update falhou' });
    });

    it('mantém data como null quando a linha atualizada não tem data', async () => {
      const supa = makeSupabaseMock([{ data: { id: '1', data: null, categoria: 'Pet', valor: '10' }, error: null }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'PUT', query: { id: '1' }, body: { valor: 10 } }, res);
      expect(res.body.lancamento.data).toBeNull();
    });
  });

  it('método não suportado retorna 405', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'PATCH', query: { id: '1' } }, res);
    expect(res.statusCode).toBe(405);
  });
});
