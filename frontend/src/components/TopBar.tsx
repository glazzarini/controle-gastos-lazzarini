import { IconeAtualizar, IconeCalendario } from './Icones';

interface Props {
  mesLabel: string;
  atualizando: boolean;
  onAbrirMeses: () => void;
  onAtualizar: () => void;
}

export function TopBar({ mesLabel, atualizando, onAbrirMeses, onAtualizar }: Props) {
  return (
    <div className="topbar">
      <div>
        <div className="topbar-title">Lazzarini 💳</div>
        <div className="topbar-sub">Controle de gastos</div>
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
        <button className="month-badge" onClick={onAbrirMeses} aria-label="Selecionar mês">
          <IconeCalendario />
          <span>{mesLabel}</span>
        </button>
        <button
          className={`refresh-btn${atualizando ? ' spinning' : ''}`}
          onClick={onAtualizar}
          disabled={atualizando}
          title="Atualizar dados"
          aria-label="Atualizar dados"
        >
          <IconeAtualizar />
        </button>
      </div>
    </div>
  );
}
