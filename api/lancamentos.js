import { createClient } from '@supabase/supabase-js';

const supabase = createClient(
  process.env.SUPABASE_URL,
  process.env.SUPABASE_SERVICE_KEY
);

function setCors(res) {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
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

  // ── GET /api/lancamentos?mes=2026-06 ─────────────────────────────────────
  if (req.method === 'GET') {
    try {
      const mes = req.query?.mes; // formato YYYY-MM
      let query = supabase
        .from('lancamentos')
        .select('*')
        .order('data', { ascending: false })
        .order('created_at', { ascending: false });

      if (mes) {
        const [y, m] = mes.split('-');
        const start = `${y}-${m}-01`;
        const end   = new Date(parseInt(y), parseInt(m), 0)
          .toISOString().split('T')[0]; // último dia do mês
        query = query.gte('data', start).lte('data', end);
      }

      const { data, error } = await query;
      if (error) throw error;

      // Retornar datas no formato BR para o frontend
      const result = data.map(l => ({ ...l, data: toBR(l.data) }));
      return res.status(200).json(result);
    } catch (err) {
      console.error('GET /api/lancamentos error:', err);
      return res.status(500).json({ error: err.message });
    }
  }

  // ── POST /api/lancamentos ─────────────────────────────────────────────────
  if (req.method === 'POST') {
    try {
      const { data: dataBR, descricao, categoria, valor, observacao, tipo, parcelas } = req.body;

      if (!dataBR || !categoria || !valor) {
        return res.status(400).json({ error: 'data, categoria e valor são obrigatórios' });
      }

      const nParcelas = parseInt(parcelas) || 1;
      const valorParcela = nParcelas > 1
        ? parseFloat((parseFloat(valor) / nParcelas).toFixed(2))
        : parseFloat(valor);

      const rows = [];
      for (let i = 0; i < nParcelas; i++) {
        // Calcular data da parcela (mês + i)
        const [d, m, y] = dataBR.split('/');
        const dt = new Date(parseInt(y), parseInt(m) - 1 + i, parseInt(d));
        const dataIso = dt.toISOString().split('T')[0];

        const descParc = nParcelas > 1
          ? `${descricao || ''} (${i + 1}/${nParcelas})`.trim()
          : (descricao || '');

        rows.push({
          data:       dataIso,
          descricao:  descParc,
          categoria,
          valor:      valorParcela,
          observacao: observacao || null,
          tipo:       tipo || 'A vista',
        });
      }

      const { data, error } = await supabase
        .from('lancamentos')
        .insert(rows)
        .select('id');

      if (error) throw error;

      return res.status(201).json({
        inserted: data.length,
        ids: data.map(r => r.id),
      });
    } catch (err) {
      console.error('POST /api/lancamentos error:', err);
      return res.status(500).json({ error: err.message });
    }
  }

  return res.status(405).json({ error: 'Method not allowed' });
}
