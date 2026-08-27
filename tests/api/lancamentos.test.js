import { describe, it, expect, vi, beforeEach } from 'vitest';
import { makeSupabaseMock, mockRes } from '../helpers/supabaseMock.js';

beforeEach(() => {
  vi.resetModules();
  vi.doMock('ws', () => ({ default: {} }));
});

async function loadHandler(supabaseMock) {
  vi.doMock('@supabase/supabase-js', () => ({ createClient: () => supabaseMock }));
  const mod = await import('../../api/lancamentos.js');
  return mod.default;
}

describe('api/lancamentos.js', () => {
  it('OPTIONS retorna 200 e encerra', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'OPTIONS', query: {} }, res);
    expect(res.statusCode).toBe(200);
    expect(res.ended).toBe(true);
  });

  describe('GET', () => {
    it('lista lançamentos do mês convertendo data para BR', async () => {
      const supa = makeSupabaseMock([
        { data: [{ id: '1', data: '2026-06-05T00:00:00Z', categoria: 'Pet', valor: '10' }], error: null },
      ]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'GET', query: { mes: '2026-06' } }, res);
      expect(res.statusCode).toBe(200);
      expect(res.body[0].data).toBe('05/06/2026');
    });

    it('lista todos os lançamentos quando mes não é informado', async () => {
      const supa = makeSupabaseMock([{ data: [], error: null }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'GET', query: {} }, res);
      expect(res.statusCode).toBe(200);
      expect(res.body).toEqual([]);
    });

    it('retorna 500 em erro do supabase', async () => {
      const supa = makeSupabaseMock([{ data: null, error: new Error('boom') }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'GET', query: {} }, res);
      expect(res.statusCode).toBe(500);
      expect(res.body).toEqual({ error: 'boom' });
    });

    it('mantém data como null quando a linha não tem data', async () => {
      const supa = makeSupabaseMock([{ data: [{ id: '1', data: null, categoria: 'Pet', valor: '10' }], error: null }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({ method: 'GET', query: {} }, res);
      expect(res.body[0].data).toBeNull();
    });
  });

  describe('POST', () => {
    it('rejeita quando faltam campos obrigatórios', async () => {
      const handler = await loadHandler(makeSupabaseMock([]));
      const res = mockRes();
      await handler({ method: 'POST', body: {} }, res);
      expect(res.statusCode).toBe(400);
    });

    it('cria lançamento à vista (1 parcela)', async () => {
      const supa = makeSupabaseMock([{ data: [{ id: 'l1' }], error: null }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({
        method: 'POST',
        body: { data: '01/06/2026', descricao: 'Almoço', categoria: 'Pet', valor: 100, observacao: 'obs', tipo: 'A vista' },
      }, res);
      expect(res.statusCode).toBe(201);
      expect(res.body).toEqual({ inserted: 1, ids: ['l1'] });
    });

    it('cria lançamento parcelado, dividindo o valor e numerando a descrição', async () => {
      const supa = makeSupabaseMock([{ data: [{ id: 'l1' }, { id: 'l2' }, { id: 'l3' }], error: null }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({
        method: 'POST',
        body: { data: '01/06/2026', descricao: 'Viagem', categoria: 'Viagem / Lazer', valor: 300, parcelas: 3 },
      }, res);
      expect(res.statusCode).toBe(201);
      expect(res.body.inserted).toBe(3);
    });

    it('cria lançamento parcelado sem descrição, numerando apenas o sufixo', async () => {
      const supa = makeSupabaseMock([{ data: [{ id: 'l1' }, { id: 'l2' }], error: null }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({
        method: 'POST',
        body: { data: '01/06/2026', categoria: 'Pet', valor: 100, parcelas: 2 },
      }, res);
      expect(res.statusCode).toBe(201);
      expect(res.body.inserted).toBe(2);
    });

    it('usa descrição e observação vazias por padrão quando não informadas', async () => {
      const supa = makeSupabaseMock([{ data: [{ id: 'l1' }], error: null }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({
        method: 'POST',
        body: { data: '01/06/2026', categoria: 'Pet', valor: 50 },
      }, res);
      expect(res.statusCode).toBe(201);
    });

    it('retorna 500 em erro do supabase', async () => {
      const supa = makeSupabaseMock([{ data: null, error: new Error('insert falhou') }]);
      const handler = await loadHandler(supa);
      const res = mockRes();
      await handler({
        method: 'POST',
        body: { data: '01/06/2026', categoria: 'Pet', valor: 50 },
      }, res);
      expect(res.statusCode).toBe(500);
      expect(res.body).toEqual({ error: 'insert falhou' });
    });
  });

  it('método não suportado retorna 405', async () => {
    const handler = await loadHandler(makeSupabaseMock([]));
    const res = mockRes();
    await handler({ method: 'DELETE', query: {} }, res);
    expect(res.statusCode).toBe(405);
  });
});
