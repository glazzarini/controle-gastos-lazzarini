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

  // ── PUT /api/categorias/:id ───────────────────────────────────────────────
  // Se o nome mudar, roda o job de migração: todos os lançamentos históricos
  // que usam o nome antigo passam a usar o novo nome (em todos os meses).
  if (req.method === 'PUT') {
    try {
      const { nome, teto } = req.body;

      const { data: atual, error: errAtual } = await getSupabase()
        .from('categorias')
        .select('*')
        .eq('id', id)
        .single();
      if (errAtual) {
        if (errAtual.code === 'PGRST116') return res.status(404).json({ error: 'Categoria não encontrada' });
        throw errAtual;
      }

      const updates = {};
      const nomeTrim = nome === undefined ? undefined : (nome || '').trim();
      if (nomeTrim !== undefined) {
        if (!nomeTrim) return res.status(400).json({ error: 'nome não pode ser vazio' });
        updates.nome = nomeTrim;
      }
      if (teto !== undefined) {
        const tetoNum = parseFloat(teto);
        if (isNaN(tetoNum) || tetoNum < 0) {
          return res.status(400).json({ error: 'teto deve ser um número maior ou igual a 0' });
        }
        updates.teto = tetoNum;
      }

      const { data, error } = await getSupabase()
        .from('categorias')
        .update(updates)
        .eq('id', id)
        .select()
        .single();

      if (error) {
        if (error.code === '23505') {
          return res.status(409).json({ error: 'Já existe uma categoria com esse nome' });
        }
        throw error;
      }

      // Job: propaga a renomeação para todo o histórico de lançamentos
      let lancamentosAtualizados = 0;
      if (nomeTrim !== undefined && nomeTrim !== atual.nome) {
        const { data: lancsAtualizados, error: errLancs } = await getSupabase()
          .from('lancamentos')
          .update({ categoria: nomeTrim })
          .eq('categoria', atual.nome)
          .select('id');
        if (errLancs) throw errLancs;
        lancamentosAtualizados = lancsAtualizados.length;
      }

      return res.status(200).json({
        updated: true,
        categoria: { ...data, teto: parseFloat(data.teto) },
        lancamentos_atualizados: lancamentosAtualizados,
      });
    } catch (err) {
      console.error('PUT /api/categorias/:id error:', err);
      return res.status(500).json({ error: err.message });
    }
  }

  // ── DELETE /api/categorias/:id ────────────────────────────────────────────
  // Bloqueia a exclusão se houver lançamentos usando essa categoria, para
  // não deixar lançamentos órfãos sem teto associado.
  if (req.method === 'DELETE') {
    try {
      const { data: atual, error: errAtual } = await getSupabase()
        .from('categorias')
        .select('nome')
        .eq('id', id)
        .single();
      if (errAtual) {
        if (errAtual.code === 'PGRST116') return res.status(404).json({ error: 'Categoria não encontrada' });
        throw errAtual;
      }

      const { count, error: errCount } = await getSupabase()
        .from('lancamentos')
        .select('id', { count: 'exact', head: true })
        .eq('categoria', atual.nome);
      if (errCount) throw errCount;

      if (count > 0) {
        return res.status(409).json({
          error: `Não é possível excluir: ${count} lançamento(s) usam essa categoria`,
          lancamentos_em_uso: count,
        });
      }

      const { error } = await getSupabase()
        .from('categorias')
        .delete()
        .eq('id', id);
      if (error) throw error;

      return res.status(200).json({ deleted: true });
    } catch (err) {
      console.error('DELETE /api/categorias/:id error:', err);
      return res.status(500).json({ error: err.message });
    }
  }

  return res.status(405).json({ error: 'Method not allowed' });
}
