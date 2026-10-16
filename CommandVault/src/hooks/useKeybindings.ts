import { useEffect, useRef } from 'react';

/**
 * Sistema de atajos global (a nivel de ventana).
 *
 * Los navegadores reservan algunos atajos y no los entregan a la página:
 * Chrome/Edge no dejan capturar Ctrl+N, Ctrl+Shift+N ni Ctrl+1..9. Por eso cada
 * acción acepta también una variante con Alt. En la app de escritorio (Tauri)
 * las variantes Ctrl funcionan directamente.
 */
export type ActionId =
  | 'goToTab' // Ctrl/Alt + 1..9  (Ctrl/Alt + 0 → Frecuentes)
  | 'newEntry' // Ctrl/Alt + N
  | 'newTab' // Ctrl/Alt + Shift + N
  | 'search' // Ctrl + F
  | 'copySelected' // Ctrl/Alt + Shift + C
  | 'copyAndDismiss' // Ctrl + Enter
  | 'nextEntry' // ↓
  | 'prevEntry' // ↑
  | 'nextTab' // Tab
  | 'prevTab'; // Shift + Tab

export type Handlers = Partial<Record<ActionId, (arg?: number) => void>>;

/** Tabla de referencia para la ayuda (Ajustes y README). */
export const SHORTCUTS: { keys: string; alt?: string; action: string }[] = [
  { keys: 'Ctrl+1 … 9', alt: 'Alt+1 … 9', action: 'Ir al tab 1º … 9º' },
  { keys: 'Ctrl+0', alt: 'Alt+0', action: 'Ir a Frecuentes' },
  { keys: 'Ctrl+N', alt: 'Alt+N', action: 'Nueva entrada en el tab activo' },
  { keys: 'Ctrl+Shift+N', alt: 'Alt+Shift+N', action: 'Nuevo tab' },
  { keys: 'Ctrl+F', action: 'Buscar en todas las entradas' },
  { keys: 'Ctrl+Shift+C', alt: 'Alt+Shift+C', action: 'Copiar la entrada seleccionada' },
  { keys: 'Ctrl+Enter', action: 'Copiar sin notificación (o cerrarla)' },
  { keys: '↑ / ↓', action: 'Navegar entre entradas' },
  { keys: 'Tab / Shift+Tab', action: 'Cambiar de tab (fuera de campos de texto)' },
  { keys: 'Enter', action: 'Copiar la entrada seleccionada' },
  { keys: 'E', action: 'Editar la entrada seleccionada' },
];

/** ¿El foco está en un control donde el teclado tiene otro significado? */
export function isTypingTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) return false;
  if (target.isContentEditable) return true;
  return ['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName);
}

/** ¿El foco está "libre" (cuerpo de la página o una tarjeta de la lista)? */
function isNavigationTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) return true;
  return target === document.body || target.closest('[data-nav-zone]') !== null;
}

/** Traduce un evento de teclado a una acción (función pura, testeable). */
export function matchShortcut(e: KeyboardEvent): { id: ActionId; arg?: number } | null {
  const mod = e.ctrlKey || e.metaKey || e.altKey;
  const key = e.key.length === 1 ? e.key.toLowerCase() : e.key;

  // e.code evita problemas con distribuciones de teclado (Shift+1 = "!" en el
  // teclado español) y con el teclado numérico.
  const digit = /^(Digit|Numpad)([0-9])$/.exec(e.code)?.[2];
  if (mod && !e.shiftKey && digit !== undefined) return { id: 'goToTab', arg: Number(digit) };

  if (mod && e.shiftKey && (key === 'n' || e.code === 'KeyN')) return { id: 'newTab' };
  if (mod && !e.shiftKey && (key === 'n' || e.code === 'KeyN')) return { id: 'newEntry' };
  if ((e.ctrlKey || e.metaKey) && !e.shiftKey && (key === 'f' || e.code === 'KeyF')) return { id: 'search' };
  if (mod && e.shiftKey && (key === 'c' || e.code === 'KeyC')) return { id: 'copySelected' };
  if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') return { id: 'copyAndDismiss' };

  if (!mod && e.key === 'ArrowDown') return { id: 'nextEntry' };
  if (!mod && e.key === 'ArrowUp') return { id: 'prevEntry' };
  if (!mod && e.key === 'Tab') return { id: e.shiftKey ? 'prevTab' : 'nextTab' };
  return null;
}

/** Acciones que no deben dispararse mientras se escribe en un campo. */
const NAVIGATION_ONLY: ReadonlySet<ActionId> = new Set(['nextEntry', 'prevEntry', 'nextTab', 'prevTab', 'copyAndDismiss']);

/**
 * Registra los atajos en `window`. `enabled = false` los suspende
 * (por ejemplo, mientras hay un diálogo abierto).
 * Los handlers se leen desde una ref para no re-suscribir en cada render.
 */
export function useKeybindings(handlers: Handlers, enabled: boolean): void {
  const ref = useRef(handlers);
  ref.current = handlers;

  useEffect(() => {
    if (!enabled) return;
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.defaultPrevented || e.isComposing) return;
      const match = matchShortcut(e);
      if (!match) return;
      if (NAVIGATION_ONLY.has(match.id) && (isTypingTarget(e.target) || !isNavigationTarget(e.target))) {
        // Excepción: Ctrl+Enter fuera de un campo, aunque el foco esté en un botón.
        if (!(match.id === 'copyAndDismiss' && !isTypingTarget(e.target))) return;
      }
      const handler = ref.current[match.id];
      if (!handler) return;
      e.preventDefault();
      handler(match.arg);
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [enabled]);
}
