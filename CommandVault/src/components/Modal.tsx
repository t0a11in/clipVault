import { useEffect, useRef, type ReactNode } from 'react';

interface Props {
  title: string;
  onClose: () => void;
  children: ReactNode;
  size?: 'sm' | 'md' | 'lg';
  /** Posición: centrado o arriba (búsqueda). */
  placement?: 'center' | 'top';
  hideHeader?: boolean;
}

/**
 * Diálogo modal accesible: Esc cierra, el foco queda atrapado dentro y vuelve
 * al elemento anterior al cerrar. Clic en el fondo también cierra.
 */
export function Modal({ title, onClose, children, size = 'md', placement = 'center', hideHeader }: Props) {
  const ref = useRef<HTMLDivElement>(null);
  const onCloseRef = useRef(onClose);
  onCloseRef.current = onClose;

  useEffect(() => {
    const previous = document.activeElement as HTMLElement | null;
    const el = ref.current;
    if (el && !el.contains(document.activeElement)) {
      const first = el.querySelector<HTMLElement>('[autofocus], input, select, textarea, button');
      first?.focus();
    }
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        e.preventDefault();
        onCloseRef.current();
      }
      if (e.key === 'Tab' && el) {
        const focusables = [...el.querySelectorAll<HTMLElement>('a[href], button, input, select, textarea, [tabindex]:not([tabindex="-1"])')].filter(
          (n) => !n.hasAttribute('disabled'),
        );
        const first = focusables[0];
        const last = focusables[focusables.length - 1];
        if (!first || !last) return;
        if (e.shiftKey && document.activeElement === first) {
          e.preventDefault();
          last.focus();
        } else if (!e.shiftKey && document.activeElement === last) {
          e.preventDefault();
          first.focus();
        }
      }
    };
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('keydown', onKey);
      previous?.focus?.({ preventScroll: true });
    };
  }, []);

  return (
    <div
      className={`overlay overlay--${placement}`}
      onMouseDown={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <div ref={ref} className={`modal modal--${size}`} role="dialog" aria-modal="true" aria-label={title}>
        {!hideHeader && (
          <header className="modal__head">
            <h2>{title}</h2>
            <button type="button" className="icon-btn" onClick={onClose} aria-label="Cerrar">
              ×
            </button>
          </header>
        )}
        {children}
      </div>
    </div>
  );
}
