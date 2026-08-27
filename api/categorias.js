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

  // ── GET /api/categorias ───────────────────────────────────────────────────
  if (req.method === 'GET') {
    try {
      const { data, error } = await getSupabase()
        .from('categorias')
        .select('*')
        .order('nome', { ascending: true });

      if (error) throw error;

      return res.status(200).json(data.map(c => ({ ...c, teto: parseFloat(c.teto) })));
    } catch (err) {
      console.error('GET /api/categorias error:', err);
      return res.status(500).json({ error: err.message });
    }
  }

  // ── POST /api/categorias ──────────────────────────────────────────────────
  if (req.method === 'POST') {
    try {
      const { nome, teto } = req.body;
      const nomeTrim = (nome || '').trim();

      if (!nomeTrim) {
        return res.status(400).json({ error: 'nome é obrigatório' });
      }
      const tetoNum = teto === undefined || teto === null || teto === '' ? 0 : parseFloat(teto);
      if (isNaN(tetoNum) || tetoNum < 0) {
        return res.status(400).json({ error: 'teto deve ser um número maior ou igual a 0' });
      }

      const { data, error } = await getSupabase()
        .from('categorias')
        .insert({ nome: nomeTrim, teto: tetoNum })
        .select()
        .single();

      if (error) {
        if (error.code === '23505') {
          return res.status(409).json({ error: 'Já existe uma categoria com esse nome' });
        }
        throw error;
      }

      return res.status(201).json({ ...data, teto: parseFloat(data.teto) });
    } catch (err) {
      console.error('POST /api/categorias error:', err);
      return res.status(500).json({ error: err.message });
    }
  }

  return res.status(405).json({ error: 'Method not allowed' });
}
