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

// Converte DD/MM/YYYY → YYYY-MM-DD para o Postgres
function toIso(dateBR) {
  if (!dateBR) return null;
  if (dateBR.includes('-')) return dateBR; // já é ISO
  const [d, m, y] = dateBR.split('/');
  return `${y}-${m}-${d}`;
}

// Converte YYYY-MM-DD → DD/MM/YYYY para o frontend
function toBR(dateIso) {
  if (!dateIso) return null;
  const [y, m, d] = String(dateIso).split('T')[0].split('-');
  return `${d}/${m}/${y}`;
}

export default async function handler(req, res) {
  setCors(res);
  if (req.method === 'OPTIONS') return res.status(200).end();

  const { id } = req.query;
  if (!id) return res.status(400).json({ error: 'ID obrigatório' });

  // ── DELETE /api/lancamentos/:id ───────────────────────────────────────────
  if (req.method === 'DELETE') {
    try {
      const { error } = await getSupabase()
        .from('lancamentos')
        .delete()
        .eq('id', id);

      if (error) throw error;
      return res.status(200).json({ deleted: true });
    } catch (err) {
      console.error('DELETE /api/lancamentos/:id error:', err);
      return res.status(500).json({ error: err.message });
    }
  }

  // ── PUT /api/lancamentos/:id ──────────────────────────────────────────────
  if (req.method === 'PUT') {
    try {
      const { data: dataBR, descricao, categoria, valor, observacao, tipo } = req.body;

      const updates = {};
      if (dataBR)     updates.data       = toIso(dataBR);
      if (descricao !== undefined) updates.descricao  = descricao;
      if (categoria)  updates.categoria  = categoria;
      if (valor !== undefined)    updates.valor       = parseFloat(valor);
      if (observacao !== undefined) updates.observacao = observacao;
      if (tipo)       updates.tipo       = tipo;

      const { data, error } = await getSupabase()
        .from('lancamentos')
        .update(updates)
        .eq('id', id)
        .select()
        .single();

      if (error) throw error;

      return res.status(200).json({
        updated: true,
        lancamento: { ...data, data: toBR(data.data) },
      });
    } catch (err) {
      console.error('PUT /api/lancamentos/:id error:', err);
      return res.status(500).json({ error: err.message });
    }
  }

  return res.status(405).json({ error: 'Method not allowed' });
}
