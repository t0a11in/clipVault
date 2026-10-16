import { useEffect } from 'react';

export interface ToastState {
  id: number;
  message: string;
  detail?: string;
  tone: 'ok' | 'warn' | 'error';
}

interface Props {
  toast: ToastState | null;
  durationMs: number;
  onClose: () => void;
}

/** Notificación breve; se cierra sola tras `durationMs` (ajuste "Auto-cerrar"). */
export function Toast({ toast, durationMs, onClose }: Props) {
  useEffect(() => {
    if (!toast) return;
    // Los avisos duran el doble: suelen requerir leer qué falta.
    const handle = window.setTimeout(onClose, toast.tone === 'ok' ? durationMs : durationMs * 2);
    return () => window.clearTimeout(handle);
  }, [toast, durationMs, onClose]);

  return (
    <div className="toast-region" role="status" aria-live="polite">
      {toast && (
        <div key={toast.id} className={`toast toast--${toast.tone}`}>
          <div>
            <p className="toast__msg">{toast.message}</p>
            {toast.detail && <p className="toast__detail">{toast.detail}</p>}
          </div>
          <button type="button" className="icon-btn icon-btn--sm" onClick={onClose} aria-label="Cerrar notificación">
            ×
          </button>
        </div>
      )}
    </div>
  );
}
