/**
 * Migração: Google Sheets → Supabase
 *
 * Uso:
 *   node scripts/migrate.js
 *
 * Requer variáveis de ambiente (ou .env):
 *   SUPABASE_URL, SUPABASE_SERVICE_KEY
 *   GOOGLE_SHEETS_ID, GOOGLE_API_KEY
 */

import { createClient } from '@supabase/supabase-js';

const SUPABASE_URL         = process.env.SUPABASE_URL;
const SUPABASE_SERVICE_KEY = process.env.SUPABASE_SERVICE_KEY;
const SHEETS_ID            = process.env.GOOGLE_SHEETS_ID;
const API_KEY              = process.env.GOOGLE_API_KEY;

const REQUIRED = { SUPABASE_URL, SUPABASE_SERVICE_KEY, GOOGLE_SHEETS_ID: SHEETS_ID, GOOGLE_API_KEY: API_KEY };
for (const [name, value] of Object.entries(REQUIRED)) {
  if (!value) {
    console.error(`❌ ${name} não definida`);
    process.exit(1);
  }
}

const supabase = createClient(SUPABASE_URL, SUPABASE_SERVICE_KEY);

// Converte DD/MM/YYYY → YYYY-MM-DD
function toIso(dateBR) {
  if (!dateBR) return null;
  if (dateBR.includes('-')) return dateBR;
  const [d, m, y] = String(dateBR).split('/');
  if (!d || !m || !y) return null;
  return `${y.trim()}-${m.trim().padStart(2,'0')}-${d.trim().padStart(2,'0')}`;
}

// Converte data que vem do Sheets como objeto Date serializado
function parseSheetDate(raw) {
  if (!raw) return null;
  // Formato DD/MM/YYYY
  if (/\d{2}\/\d{2}\/\d{4}/.test(raw)) return toIso(raw);
  // Formato que o Sheets às vezes exporta: "Mon Jun 01 2026 00:00:00 GMT..."
  const d = new Date(raw);
  if (!isNaN(d.getTime())) {
    const day   = String(d.getDate()).padStart(2,'0');
    const month = String(d.getMonth()+1).padStart(2,'0');
    return `${d.getFullYear()}-${month}-${day}`;
  }
  return null;
}

async function fetchSheet(range) {
  const url = `https://sheets.googleapis.com/v4/spreadsheets/${SHEETS_ID}/values/${encodeURIComponent(range)}?key=${API_KEY}`;
  const res = await fetch(url);
  if (!res.ok) throw new Error(`Sheets API error: ${res.status} ${await res.text()}`);
  const json = await res.json();
  return json.values || [];
}

async function migrateLancamentos() {
  console.log('\n📋 Buscando lançamentos do Google Sheets...');
  const rows = await fetchSheet('lancamentos!A:G');

  if (rows.length < 2) {
    console.log('   Nenhum lançamento encontrado.');
    return 0;
  }

  // Pula o cabeçalho (linha 0)
  const records = rows.slice(1).map(row => {
    const [data, descricao, categoria, valor, estabelecimento, tipo, obs] = row;
    const dataIso = parseSheetDate(data);
    if (!dataIso || !categoria || !valor) return null;

    return {
      data:       dataIso,
      descricao:  descricao  || '',
      categoria:  categoria  || '',
      valor:      parseFloat(String(valor).replace(',', '.')) || 0,
      // estabelecimento era o campo que virou observacao
      observacao: estabelecimento || obs || null,
      tipo:       tipo || 'A vista',
    };
  }).filter(Boolean);

  console.log(`   ${records.length} registros válidos encontrados`);

  // Inserir em lotes de 100
  const BATCH = 100;
  let inserted = 0;
  let errors   = 0;

  for (let i = 0; i < records.length; i += BATCH) {
    const batch = records.slice(i, i + BATCH);
    const { error } = await supabase.from('lancamentos').insert(batch);
    if (error) {
      console.error(`   ❌ Erro no lote ${i}–${i+BATCH}:`, error.message);
      errors += batch.length;
    } else {
      inserted += batch.length;
      process.stdout.write(`   ✅ ${inserted}/${records.length}\r`);
    }
  }

  console.log(`\n   Inseridos: ${inserted} | Erros: ${errors}`);
  return inserted;
}

async function migrateConfig() {
  console.log('\n⚙️  Buscando config do Google Sheets...');
  const rows = await fetchSheet('config!A:B');

  if (rows.length === 0) {
    console.log('   Nenhuma config encontrada.');
    return;
  }

  const records = rows.map(([chave, valor]) => ({ chave, valor: String(valor) })).filter(r => r.chave);

  const { error } = await supabase
    .from('config')
    .upsert(records, { onConflict: 'chave' });

  if (error) {
    console.error('   ❌ Erro ao migrar config:', error.message);
  } else {
    console.log(`   ✅ ${records.length} configs migradas`);
  }
}

async function main() {
  console.log('🚀 Iniciando migração Google Sheets → Supabase');
  console.log(`   Sheets ID: ${SHEETS_ID}`);
  console.log(`   Supabase:  ${SUPABASE_URL}`);

  const start = Date.now();

  await migrateConfig();
  const total = await migrateLancamentos();

  const elapsed = ((Date.now() - start) / 1000).toFixed(1);
  console.log(`\n✅ Migração concluída em ${elapsed}s — ${total} lançamentos migrados`);
}

main().catch(err => {
  console.error('\n❌ Falha na migração:', err);
  process.exit(1);
});
