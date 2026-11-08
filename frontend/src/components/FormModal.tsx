import { useEffect, useState, type MouseEvent } from 'react';
import { useUi } from '../state/Ui';

export interface CampoForm {
  chave: string;
  rotulo: string;
  tipo: 'text' | 'money';
  valor: string | number;
}

export interface ConfigFormModal {
  titulo: string;
  campos: CampoForm[];
  aoSalvar: (valores: Record<string, string>) => Promise<void>;
}

/** Modal genérico (renda / conta fixa / categoria) — fecha só quando o salvamento dá certo. */
export function FormModal({ config, onFechar }: { config: ConfigFormModal | null; onFechar: () => void }) {
  const { toast } = useUi();
  const [valores, setValores] = useState<Record<string, string>>({});
  const [salvando, setSalvando] = useState(false);

  useEffect(() => {
    if (config) setValores(Object.fromEntries(config.campos.map(c => [c.chave, String(c.valor)])));
  }, [config]);

  const aoClicarOverlay = (e: MouseEvent) => { if (e.target === e.currentTarget) onFechar(); };

  const salvar = async () => {
    if (!config) return;
    setSalvando(true);
    try {
      await config.aoSalvar(valores);
      onFechar();
    } catch (e) {
      toast(`Erro: ${(e as Error).message}`, 'error');
    } finally {
      setSalvando(false);
    }
  };

  return (
    <div className={`edit-modal-overlay${config ? ' open' : ''}`} onClick={aoClicarOverlay} data-testid="form-modal">
      <div className="edit-modal-sheet">
        <div className="edit-modal-title">{config?.titulo ?? 'Editar'}</div>
        {config?.campos.map(c => (
          <div className="form-group" key={c.chave}>
            <label className="form-label" htmlFor={`form-${c.chave}`}>{c.rotulo}</label>
            <input
              id={`form-${c.chave}`}
              className="form-input"
              type={c.tipo === 'money' ? 'number' : 'text'}
              step={c.tipo === 'money' ? '0.01' : undefined}
              min={c.tipo === 'money' ? '0' : undefined}
              value={valores[c.chave] ?? ''}
              onChange={e => setValores(v => ({ ...v, [c.chave]: e.target.value }))}
            />
          </div>
        ))}
        <button className="edit-save-btn" onClick={salvar} disabled={salvando}>
          {salvando ? 'Salvando...' : 'Salvar'}
        </button>
      </div>
    </div>
  );
}
