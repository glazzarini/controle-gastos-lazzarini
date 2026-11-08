import type { Config, ContaFixaResumo } from './types';

// Valores locais usados enquanto a API não responde (ou se falhar) — mesmos do index.html.

export const TETOS_PADRAO: Record<string, number> = {
  'Alimentacao fora': 2000,
  'Mercado / Supermercado': 1800,
  'Farmacia / Saude': 800,
  'Veiculos / Transporte': 1400,
  'Viagem / Lazer': 1000,
  'Vestuario / Moda': 1000,
  'Mercado Livre': 600,
  'Streaming / Assinaturas': 370,
  'Educacao / Infanto': 180,
  'Pet': 450,
  'Decoracao Casa': 800,
  'Guitarra': 300,
  'Presentes': 800,
  'Outros / Diversos': 500,
};

export const CONTAS_FIXAS_PADRAO: ContaFixaResumo[] = [
  { nome: 'Financiamento imóvel', valor: 7500, icone: '🏠' },
  { nome: 'Escola (filha)', valor: 2300, icone: '🏫' },
  { nome: 'Convênio médico', valor: 2000, icone: '🏥' },
  { nome: 'Condomínio', valor: 1600, icone: '🏢' },
  { nome: 'Terapia', valor: 510, icone: '🧠' },
  { nome: 'Claro', valor: 281, icone: '📱' },
  { nome: 'Vivo', valor: 124, icone: '📱' },
  { nome: 'Seguro carro', valor: 244, icone: '🚗' },
];

export const CONFIG_PADRAO: Config = {
  renda_guilherme: 20000,
  renda_esposa: 14000,
  meta_fatura: 12000,
  total_fixos: 14559,
};

export const META_FATURA_PADRAO = 12000;

export const CAT_ICONS: Record<string, string> = {
  'Alimentacao fora': '🍽', 'Mercado / Supermercado': '🛒',
  'Farmacia / Saude': '💊', 'Veiculos / Transporte': '🚗',
  'Viagem / Lazer': '✈', 'Vestuario / Moda': '🛍',
  'Mercado Livre': '📦', 'Streaming / Assinaturas': '📺',
  'Educacao / Infanto': '📚', 'Pet': '🐾',
  'Decoracao Casa': '🏠', 'Guitarra': '🎸',
  'Presentes': '🎁', 'Outros / Diversos': '🎯',
};

export function iconeCategoria(categoria: string): string {
  return CAT_ICONS[categoria] || '💳';
}
