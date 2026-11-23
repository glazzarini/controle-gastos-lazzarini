// Tipos do contrato HTTP (/api/*) — snake_case, datas DD/MM/YYYY, dinheiro como number.

export type TipoLancamento = 'A vista' | 'Parcelado';

export interface LancamentoApi {
  id: string;
  data: string | null;
  descricao: string | null;
  categoria: string;
  valor: number;
  observacao: string | null;
  tipo: string | null;
  created_at: string | null;
}

export interface ContaFixaResumo {
  id?: string;
  nome: string;
  valor: number;
  /** Só existe nos valores padrão locais (fallback quando a API falha). */
  icone?: string;
}

export interface ConfigApi {
  renda_guilherme: number;
  renda_esposa: number;
  meta_fatura: number;
  total_fixos: number;
  tetos: Record<string, number>;
  contas_fixas: ContaFixaResumo[];
}

export interface Categoria {
  id: string;
  nome: string;
  teto: number;
  created_at: string | null;
}

export interface ContaFixa {
  id: string;
  nome: string;
  valor: number;
  created_at: string | null;
}

export interface LancamentoNovo {
  data: string;
  descricao: string;
  categoria: string;
  valor: number;
  observacao: string;
  tipo: string;
  parcelas: number;
}

export interface LancamentoEdicao {
  data: string;
  descricao: string;
  categoria: string;
  valor: number;
  observacao: string;
  tipo: string;
}

export interface LancamentosCriados { inserted: number; ids: string[] }
export interface LancamentoAtualizado { updated: true; lancamento: LancamentoApi }
export interface CategoriaAtualizada { updated: true; categoria: Categoria; lancamentos_atualizados: number }
export interface ContaFixaAtualizada { updated: true; conta_fixa: ContaFixa }
export interface Updated { updated: true }
export interface Deleted { deleted: true }

/** Lançamento já convertido para uso nas telas (data como Date local). */
export interface Lancamento {
  id: string;
  data: Date;
  descricao: string;
  categoria: string;
  valor: number;
  observacao: string;
  tipo: string;
}

export interface Config {
  renda_guilherme: number;
  renda_esposa: number;
  meta_fatura: number;
  total_fixos: number;
}
