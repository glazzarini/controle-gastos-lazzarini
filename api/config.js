import { createClient } from '@supabase/supabase-js';
import ws from 'ws';

let supabase;
function getSupabase() {
  if (!supabase) {
    supabase = createClient(process.env.SUPABASE_URL, process.env.SUPABASE_SERVICE_KEY, {
      realtime: { transport: ws },
    });
  }
  return supabase;
}

export default async function handler(req, res) {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, PUT, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

  if (req.method === 'OPTIONS') return res.status(200).end();

  // ── GET /api/config ────────────────────────────────────────────────────────
  if (req.method === 'GET') {
    try {
      const [{ data: cfgRows, error: errCfg }, { data: categorias, error: errCat }, { data: contasFixas, error: errCf }] =
        await Promise.all([
          getSupabase().from('config').select('*'),
          getSupabase().from('categorias').select('nome, teto').order('nome', { ascending: true }),
          getSupabase().from('contas_fixas').select('id, nome, valor').order('created_at', { ascending: true }),
        ]);
      if (errCfg) throw errCfg;
      if (errCat) throw errCat;
      if (errCf) throw errCf;

      const map = {};
      cfgRows.forEach(({ chave, valor }) => { map[chave] = valor; });

      const tetos = {};
      categorias.forEach(({ nome, teto }) => { tetos[nome] = parseFloat(teto); });

      const contas_fixas = contasFixas.map(({ id, nome, valor }) => ({ id, nome, valor: parseFloat(valor) }));
      const total_fixos = contas_fixas.reduce((acc, c) => acc + c.valor, 0);

      return res.status(200).json({
        renda_guilherme: parseFloat(map.renda_guilherme || 0),
        renda_esposa:    parseFloat(map.renda_esposa    || 0),
        meta_fatura:     parseFloat(map.meta_fatura     || 12000),
        total_fixos,
        tetos,
        contas_fixas,
      });
    } catch (err) {
      console.error('GET /api/config error:', err);
      return res.status(500).json({ error: err.message });
    }
  }

  // ── PUT /api/config ────────────────────────────────────────────────────────
  // Atualiza renda_guilherme e/ou renda_esposa. Como config é global (sem
  // histórico por mês), a mudança já vale para todos os meses automaticamente.
  if (req.method === 'PUT') {
    try {
      const { renda_guilherme, renda_esposa } = req.body;
      const updates = [];

      if (renda_guilherme !== undefined) {
        const n = parseFloat(renda_guilherme);
        if (isNaN(n) || n < 0) return res.status(400).json({ error: 'renda_guilherme deve ser um número maior ou igual a 0' });
        updates.push({ chave: 'renda_guilherme', valor: String(n) });
      }
      if (renda_esposa !== undefined) {
        const n = parseFloat(renda_esposa);
        if (isNaN(n) || n < 0) return res.status(400).json({ error: 'renda_esposa deve ser um número maior ou igual a 0' });
        updates.push({ chave: 'renda_esposa', valor: String(n) });
      }

      if (updates.length === 0) {
        return res.status(400).json({ error: 'Informe renda_guilherme e/ou renda_esposa' });
      }

      const { error } = await getSupabase().from('config').upsert(updates, { onConflict: 'chave' });
      if (error) throw error;

      return res.status(200).json({ updated: true });
    } catch (err) {
      console.error('PUT /api/config error:', err);
      return res.status(500).json({ error: err.message });
    }
  }

  return res.status(405).json({ error: 'Method not allowed' });
}
