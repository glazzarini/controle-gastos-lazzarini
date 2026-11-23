import type {
  Categoria, CategoriaAtualizada, ConfigApi, ContaFixa, ContaFixaAtualizada, Deleted,
  LancamentoApi, LancamentoAtualizado, LancamentoEdicao, LancamentoNovo, LancamentosCriados, Updated,
} from './types';

export class ApiError extends Error {
  constructor(message: string, readonly status: number) {
    super(message);
    this.name = 'ApiError';
  }
}

/** Mesmo comportamento do apiFetch do index.html: erro = body.error ou mensagem genérica. */
export async function apiFetch<T>(url: string, options?: RequestInit): Promise<T> {
  const resposta = await fetch(url, options);
  const corpo: unknown = await resposta.json().catch(() => ({}));
  if (!resposta.ok) {
    const erro = (corpo as { error?: unknown } | null)?.error;
    throw new ApiError(typeof erro === 'string' && erro ? erro : 'Erro na requisição', resposta.status);
  }
  return corpo as T;
}

function comJson(method: 'POST' | 'PUT', body: unknown): RequestInit {
  return { method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) };
}

const DELETE: RequestInit = { method: 'DELETE' };

export const api = {
  obterConfig: () => apiFetch<ConfigApi>('/api/config'),
  atualizarRenda: (renda: { renda_guilherme: number; renda_esposa: number }) =>
    apiFetch<Updated>('/api/config', comJson('PUT', renda)),

  listarLancamentos: () => apiFetch<LancamentoApi[]>('/api/lancamentos'),
  criarLancamento: (novo: LancamentoNovo) => apiFetch<LancamentosCriados>('/api/lancamentos', comJson('POST', novo)),
  atualizarLancamento: (id: string, edicao: LancamentoEdicao) =>
    apiFetch<LancamentoAtualizado>(`/api/lancamentos/${id}`, comJson('PUT', edicao)),
  excluirLancamento: (id: string) => apiFetch<Deleted>(`/api/lancamentos/${id}`, DELETE),

  listarCategorias: () => apiFetch<Categoria[]>('/api/categorias'),
  criarCategoria: (c: { nome: string; teto: number }) => apiFetch<Categoria>('/api/categorias', comJson('POST', c)),
  atualizarCategoria: (id: string, c: { nome: string; teto: number }) =>
    apiFetch<CategoriaAtualizada>(`/api/categorias/${id}`, comJson('PUT', c)),
  excluirCategoria: (id: string) => apiFetch<Deleted>(`/api/categorias/${id}`, DELETE),

  criarContaFixa: (c: { nome: string; valor: number }) => apiFetch<ContaFixa>('/api/contas-fixas', comJson('POST', c)),
  atualizarContaFixa: (id: string, c: { nome: string; valor: number }) =>
    apiFetch<ContaFixaAtualizada>(`/api/contas-fixas/${id}`, comJson('PUT', c)),
  excluirContaFixa: (id: string) => apiFetch<Deleted>(`/api/contas-fixas/${id}`, DELETE),
};
