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
  res.setHeader('Access-Control-Allow-Methods', 'PUT, DELETE, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
}

export default async function handler(req, res) {
  setCors(res);
  if (req.method === 'OPTIONS') return res.status(200).end();

  const { id } = req.query;
  if (!id) return res.status(400).json({ error: 'ID obrigatório' });

  // ── PUT /api/contas-fixas/:id ─────────────────────────────────────────────
  if (req.method === 'PUT') {
    try {
      const { nome, valor } = req.body;

      const updates = {};
      if (nome !== undefined) {
        const nomeTrim = (nome || '').trim();
        if (!nomeTrim) return res.status(400).json({ error: 'nome não pode ser vazio' });
        updates.nome = nomeTrim;
      }
      if (valor !== undefined) {
        const valorNum = parseFloat(valor);
        if (isNaN(valorNum) || valorNum < 0) {
          return res.status(400).json({ error: 'valor deve ser um número maior ou igual a 0' });
        }
        updates.valor = valorNum;
      }

      const { data, error } = await getSupabase()
        .from('contas_fixas')
        .update(updates)
        .eq('id', id)
        .select()
        .single();

      if (error) {
        if (error.code === 'PGRST116') return res.status(404).json({ error: 'Conta fixa não encontrada' });
        throw error;
      }

      return res.status(200).json({ updated: true, conta_fixa: { ...data, valor: parseFloat(data.valor) } });
    } catch (err) {
      console.error('PUT /api/contas-fixas/:id error:', err);
      return res.status(500).json({ error: err.message });
    }
  }

  // ── DELETE /api/contas-fixas/:id ──────────────────────────────────────────
  if (req.method === 'DELETE') {
    try {
      const { error } = await getSupabase()
        .from('contas_fixas')
        .delete()
        .eq('id', id);
      if (error) throw error;

      return res.status(200).json({ deleted: true });
    } catch (err) {
      console.error('DELETE /api/contas-fixas/:id error:', err);
      return res.status(500).json({ error: err.message });
    }
  }

  return res.status(405).json({ error: 'Method not allowed' });
}
