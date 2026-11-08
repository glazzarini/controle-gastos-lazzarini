import { createContext, useContext } from 'react';
import type { Mes } from '../lib/meses';

export type Tela = 'painel' | 'lancar' | 'lancamentos' | 'orcamento' | 'historico' | 'detalhe';

export interface Navegacao {
  tela: Tela;
  mes: Mes;
  irPara: (tela: Exclude<Tela, 'detalhe'>) => void;
  abrirDetalhe: (id: string) => void;
}

export const NavegacaoContext = createContext<Navegacao | null>(null);

export function useNavegacao(): Navegacao {
  const contexto = useContext(NavegacaoContext);
  if (!contexto) throw new Error('useNavegacao fora do App');
  return contexto;
}
