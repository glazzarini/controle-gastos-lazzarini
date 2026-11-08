import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from 'react';

export type TipoToast = 'success' | 'error';

export interface Ui {
  toast: (mensagem: string, tipo?: TipoToast) => void;
  confirmar: (titulo: string, texto: string, aoConfirmar: () => void) => void;
}

const UiContext = createContext<Ui | null>(null);

interface EstadoConfirmacao { titulo: string; texto: string; aoConfirmar: (() => void) | null; aberto: boolean }

/** Toast (2,5s) e modal de confirmação compartilhados por todas as telas. */
export function UiProvider({ children }: { children: ReactNode }) {
  const [toastAtual, setToastAtual] = useState({ mensagem: '', tipo: 'success' as TipoToast, visivel: false });
  const [confirmacao, setConfirmacao] = useState<EstadoConfirmacao>({ titulo: 'Confirmar', texto: '', aoConfirmar: null, aberto: false });
  const timer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  const toast = useCallback((mensagem: string, tipo: TipoToast = 'success') => {
    clearTimeout(timer.current);
    setToastAtual({ mensagem, tipo, visivel: true });
    timer.current = setTimeout(() => setToastAtual(t => ({ ...t, visivel: false })), 2500);
  }, []);

  const confirmar = useCallback((titulo: string, texto: string, aoConfirmar: () => void) => {
    setConfirmacao({ titulo, texto, aoConfirmar, aberto: true });
  }, []);

  const fechar = () => setConfirmacao(c => ({ ...c, aoConfirmar: null, aberto: false }));
  const ok = () => {
    confirmacao.aoConfirmar?.();
    fechar();
  };

  const valor = useMemo(() => ({ toast, confirmar }), [toast, confirmar]);

  return (
    <UiContext.Provider value={valor}>
      {children}
      <div className={`confirm-modal${confirmacao.aberto ? ' open' : ''}`} role="dialog" aria-hidden={!confirmacao.aberto} aria-label={confirmacao.titulo}>
        <div className="confirm-box">
          <div className="confirm-title">{confirmacao.titulo}</div>
          <div className="confirm-text">{confirmacao.texto}</div>
          <div className="confirm-btns">
            <button className="confirm-cancel" onClick={fechar}>Cancelar</button>
            <button className="confirm-ok" onClick={ok}>Confirmar</button>
          </div>
        </div>
      </div>
      <div className={`toast ${toastAtual.tipo}${toastAtual.visivel ? ' show' : ''}`} role="status">{toastAtual.mensagem}</div>
    </UiContext.Provider>
  );
}

export function useUi(): Ui {
  const contexto = useContext(UiContext);
  if (!contexto) throw new Error('useUi fora do UiProvider');
  return contexto;
}
