import type { ReactNode } from 'react';
import type { Tela } from '../state/Navegacao';
import { IconeHistorico, IconeLancamentos, IconeLancar, IconeOrcamento, IconePainel } from './Icones';

export type TelaNav = Exclude<Tela, 'detalhe'>;

const BOTOES: { tela: TelaNav; rotulo: string; icone: ReactNode }[] = [
  { tela: 'painel', rotulo: 'Painel', icone: <IconePainel /> },
  { tela: 'lancar', rotulo: 'Lançar', icone: <IconeLancar /> },
  { tela: 'lancamentos', rotulo: 'Lançamentos', icone: <IconeLancamentos /> },
  { tela: 'orcamento', rotulo: 'Orçamento', icone: <IconeOrcamento /> },
  { tela: 'historico', rotulo: 'Histórico', icone: <IconeHistorico /> },
];

export function BottomNav({ ativa, onIr }: { ativa: TelaNav; onIr: (tela: TelaNav) => void }) {
  return (
    <nav className="nav">
      {BOTOES.map(b => (
        <button key={b.tela} className={`nav-btn${b.tela === ativa ? ' active' : ''}`} onClick={() => onIr(b.tela)}>
          {b.icone}
          {b.rotulo}
        </button>
      ))}
    </nav>
  );
}
