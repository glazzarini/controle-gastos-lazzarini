import { vi } from 'vitest';

// Fabrica um mock do client do Supabase (@supabase/supabase-js createClient()).
// `responses` é consumido em ordem: cada chamada a `.from(...)` que acontece
// no handler consome o próximo item do array como resultado final da chain
// (o que `await query` resolveria para {data, error} / {data, error, count}).
export function makeSupabaseMock(responses) {
  let i = 0;
  const calls = [];

  const from = vi.fn((table) => {
    if (i >= responses.length) {
      throw new Error(`makeSupabaseMock: nenhuma resposta configurada para a chamada #${i + 1} (from("${table}"))`);
    }
    const result = responses[i++];
    const call = { table, methods: [] };
    calls.push(call);

    const builder = {};
    ['select', 'insert', 'update', 'delete', 'upsert', 'eq', 'gte', 'lte', 'order', 'single'].forEach((method) => {
      builder[method] = vi.fn((...args) => {
        call.methods.push({ method, args });
        return builder;
      });
    });
    builder.then = (resolve, reject) => Promise.resolve(result).then(resolve, reject);
    return builder;
  });

  return { from, __calls: calls };
}

export function mockRes() {
  const res = {
    statusCode: 200,
    headers: {},
    body: undefined,
    ended: false,
  };
  res.setHeader = vi.fn((k, v) => { res.headers[k] = v; });
  res.status = vi.fn((code) => { res.statusCode = code; return res; });
  res.json = vi.fn((body) => { res.body = body; return res; });
  res.end = vi.fn(() => { res.ended = true; return res; });
  return res;
}
