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

function setCors(res) {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
}

export default async function handler(req, res) {
  setCors(res);
  if (req.method === 'OPTIONS') return res.status(200).end();

  // ── GET /api/contas-fixas ─────────────────────────────────────────────────
  if (req.method === 'GET') {
    try {
      const { data, error } = await getSupabase()
        .from('contas_fixas')
        .select('*')
        .order('created_at', { ascending: true });

      if (error) throw error;

      return res.status(200).json(data.map(c => ({ ...c, valor: parseFloat(c.valor) })));
    } catch (err) {
      console.error('GET /api/contas-fixas error:', err);
      return res.status(500).json({ error: err.message });
    }
  }

  // ── POST /api/contas-fixas ────────────────────────────────────────────────
  if (req.method === 'POST') {
    try {
      const { nome, valor } = req.body;
      const nomeTrim = (nome || '').trim();

      if (!nomeTrim) {
        return res.status(400).json({ error: 'nome é obrigatório' });
      }
      const valorNum = parseFloat(valor);
      if (isNaN(valorNum) || valorNum < 0) {
        return res.status(400).json({ error: 'valor deve ser um número maior ou igual a 0' });
      }

      const { data, error } = await getSupabase()
        .from('contas_fixas')
        .insert({ nome: nomeTrim, valor: valorNum })
        .select()
        .single();

      if (error) throw error;

      return res.status(201).json({ ...data, valor: parseFloat(data.valor) });
    } catch (err) {
      console.error('POST /api/contas-fixas error:', err);
      return res.status(500).json({ error: err.message });
    }
  }

  return res.status(405).json({ error: 'Method not allowed' });
}
