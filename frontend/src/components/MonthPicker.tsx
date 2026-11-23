import type { MouseEvent } from 'react';
import type { Mes } from '../lib/meses';

interface Props {
  aberto: boolean;
  meses: Mes[];
  atual: Mes;
  onSelecionar: (mes: Mes) => void;
  onFechar: () => void;
}

export function MonthPicker({ aberto, meses, atual, onSelecionar, onFechar }: Props) {
  // Fecha só ao clicar fora da folha (no overlay), como no index.html.
  const aoClicarOverlay = (e: MouseEvent) => { if (e.target === e.currentTarget) onFechar(); };
  return (
    <div className={`modal-overlay${aberto ? ' open' : ''}`} onClick={aoClicarOverlay} data-testid="month-modal">
      <div className="modal-sheet">
        <div className="modal-title">Selecionar mês</div>
        <div className="month-grid">
          {meses.map(m => (
            <div
              key={m.label}
              role="button"
              className={`month-opt${m.label === atual.label ? ' active' : ''}`}
              onClick={() => onSelecionar(m)}
            >
              {m.label}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
