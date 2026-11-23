import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { api } from '../lib/api';
import { parseDate } from '../lib/datas';
import { CONFIG_PADRAO, CONTAS_FIXAS_PADRAO, TETOS_PADRAO } from '../lib/defaults';
import type { Categoria, Config, ContaFixaResumo, Lancamento } from '../lib/types';

export interface AppData {
  config: Config;
  tetos: Record<string, number>;
  contasFixas: ContaFixaResumo[];
  categorias: Categoria[];
  lancamentos: Lancamento[];
  carregado: boolean;
  /** Recarrega tudo da API (equivalente ao loadData do index.html). */
  recarregar: () => Promise<void>;
}

const AppDataContext = createContext<AppData | null>(null);

interface Estado {
  config: Config;
  tetos: Record<string, number>;
  contasFixas: ContaFixaResumo[];
  categorias: Categoria[];
  lancamentos: Lancamento[];
}

const ESTADO_INICIAL: Estado = {
  config: CONFIG_PADRAO,
  tetos: TETOS_PADRAO,
  contasFixas: CONTAS_FIXAS_PADRAO,
  categorias: [],
  lancamentos: [],
};

/** Cada chamada falha de forma isolada e mantém o valor anterior, como no index.html. */
async function carregar(anterior: Estado): Promise<Estado> {
  const proximo = { ...anterior };

  try {
    const cfg = await api.obterConfig();
    proximo.config = {
      renda_guilherme: cfg.renda_guilherme,
      renda_esposa: cfg.renda_esposa,
      meta_fatura: cfg.meta_fatura,
      total_fixos: cfg.total_fixos,
    };
    if (Object.keys(cfg.tetos).length) proximo.tetos = cfg.tetos;
    if (cfg.contas_fixas.length) proximo.contasFixas = cfg.contas_fixas;
  } catch (e) {
    console.error(e); // mantém os valores locais
  }

  try {
    proximo.categorias = await api.listarCategorias();
  } catch (e) {
    console.error(e); // tela de orçamento mostra a lista vazia
  }

  try {
    const linhas = await api.listarLancamentos();
    proximo.lancamentos = linhas
      .map(item => ({
        id: item.id,
        data: parseDate(item.data),
        descricao: item.descricao || '',
        categoria: item.categoria || '',
        valor: item.valor,
        observacao: item.observacao || '',
        tipo: item.tipo || 'A vista',
      }))
      .sort((a, b) => b.data.getTime() - a.data.getTime());
  } catch (e) {
    console.error(e);
    proximo.lancamentos = [];
  }

  return proximo;
}

export function AppDataProvider({ children }: { children: ReactNode }) {
  const [estado, setEstado] = useState<Estado>(ESTADO_INICIAL);
  const [carregado, setCarregado] = useState(false);
  // Último estado carregado: base para manter valores quando uma chamada falha.
  const ultimo = useRef<Estado>(ESTADO_INICIAL);

  const recarregar = useCallback(async () => {
    const novo = await carregar(ultimo.current);
    ultimo.current = novo;
    setEstado(novo);
    setCarregado(true);
  }, []);

  useEffect(() => { void recarregar(); }, [recarregar]);

  const valor = useMemo(() => ({ ...estado, carregado, recarregar }), [estado, carregado, recarregar]);
  return <AppDataContext.Provider value={valor}>{children}</AppDataContext.Provider>;
}

export function useAppData(): AppData {
  const contexto = useContext(AppDataContext);
  if (!contexto) throw new Error('useAppData fora do AppDataProvider');
  return contexto;
}
