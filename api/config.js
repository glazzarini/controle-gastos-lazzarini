import { createClient } from '@supabase/supabase-js';

const supabase = createClient(
  process.env.SUPABASE_URL,
  process.env.SUPABASE_SERVICE_KEY
);

// Mapeamento chave do banco → nome da categoria no app
const TETO_MAP = {
  'teto_alimentacao_fora':      'Alimentacao fora',
  'teto_mercado_supermercado':  'Mercado / Supermercado',
  'teto_farmacia_saude':        'Farmacia / Saude',
  'teto_veiculos_transporte':   'Veiculos / Transporte',
  'teto_viagem_lazer':          'Viagem / Lazer',
  'teto_vestuario_moda':        'Vestuario / Moda',
  'teto_mercado_livre':         'Mercado Livre',
  'teto_streaming_assinaturas': 'Streaming / Assinaturas',
  'teto_educacao_infanto':      'Educacao / Infanto',
  'teto_pet':                   'Pet',
  'teto_decoracao_casa':        'Decoracao Casa',
  'teto_guitarra':              'Guitarra',
  'teto_presentes':             'Presentes',
  'teto_outros_diversos':       'Outros / Diversos',
};

// Mapeamento chave do banco → nome de exibição da conta fixa
const CONTA_FIXA_MAP = {
  conta_fixa_financiamento_imovel: 'Financiamento imóvel',
  conta_fixa_escola:               'Escola (filha)',
  conta_fixa_convenio_medico:      'Convênio médico',
  conta_fixa_condominio:           'Condomínio',
  conta_fixa_terapia:              'Terapia',
  conta_fixa_claro:                'Claro',
  conta_fixa_vivo:                 'Vivo',
  conta_fixa_seguro_carro:         'Seguro carro',
};

export default async function handler(req, res) {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

  if (req.method === 'OPTIONS') return res.status(200).end();
  if (req.method !== 'GET') return res.status(405).json({ error: 'Method not allowed' });

  try {
    const { data, error } = await supabase.from('config').select('*');
    if (error) throw error;

    const map = {};
    data.forEach(({ chave, valor }) => { map[chave] = valor; });

    const tetos = {};
    Object.entries(TETO_MAP).forEach(([chave, cat]) => {
      tetos[cat] = parseFloat(map[chave] || 0);
    });

    const contas_fixas = Object.entries(CONTA_FIXA_MAP).map(([chave, nome]) => ({
      nome,
      valor: parseFloat(map[chave] || 0),
    }));

    return res.status(200).json({
      renda_guilherme: parseFloat(map.renda_guilherme || 0),
      renda_esposa:    parseFloat(map.renda_esposa    || 0),
      meta_fatura:     parseFloat(map.meta_fatura     || 12000),
      total_fixos:     parseFloat(map.total_fixos     || 0),
      tetos,
      contas_fixas,
    });
  } catch (err) {
    console.error('GET /api/config error:', err);
    return res.status(500).json({ error: err.message });
  }
}
