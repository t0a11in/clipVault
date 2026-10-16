import { useId, useLayoutEffect, useMemo, useRef, useState, type KeyboardEvent } from 'react';
import type { Variable } from '../types/model';
import { variableTokenAt } from '../domain/variables';

interface Props {
  value: string;
  onChange: (value: string) => void;
  /** Variables de la entrada (borrador actual). */
  variables: Variable[];
  globals: Variable[];
  autoFocus?: boolean;
  /** Ctrl+Enter / Ctrl+S cuando el menú de sugerencias está cerrado. */
  onSubmit?: () => void;
  invalid?: boolean;
}

interface Suggestion {
  name: string;
  scope: 'entrada' | 'global';
  sensitive: boolean;
}

/**
 * Campo de comando con autocompletado: al escribir "$" sugiere las variables
 * de la entrada y las globales. ↑↓ elige, Enter/Tab acepta, Esc cierra.
 */
export function CommandInput({ value, onChange, variables, globals, autoFocus, onSubmit, invalid }: Props) {
  const ref = useRef<HTMLTextAreaElement>(null);
  const listId = useId();
  const [caret, setCaret] = useState<number | null>(null);
  const [active, setActive] = useState(0);
  const [dismissed, setDismissed] = useState(false);
  // Posición del cursor a restaurar tras aceptar una sugerencia. Se aplica en
  // useLayoutEffect (antes de que el navegador procese la siguiente tecla); con
  // requestAnimationFrame, al escribir rápido la tecla caía al final del texto.
  const pendingCaret = useRef<number | null>(null);
  useLayoutEffect(() => {
    const pos = pendingCaret.current;
    if (pos !== null && ref.current) {
      ref.current.setSelectionRange(pos, pos);
      pendingCaret.current = null;
    }
  }, [value]);

  const token = caret === null ? null : variableTokenAt(value, caret);

  const suggestions = useMemo<Suggestion[]>(() => {
    if (!token) return [];
    const prefix = token.prefix.toLowerCase();
    const seen = new Set<string>();
    const all: Suggestion[] = [];
    for (const v of variables) {
      if (v.name && !seen.has(v.name)) {
        seen.add(v.name);
        all.push({ name: v.name, scope: 'entrada', sensitive: v.sensitive });
      }
    }
    for (const v of globals) {
      if (!seen.has(v.name)) {
        seen.add(v.name);
        all.push({ name: v.name, scope: 'global', sensitive: v.sensitive });
      }
    }
    return all.filter((s) => s.name.toLowerCase().startsWith(prefix) && s.name !== token.prefix);
  }, [token, variables, globals]);

  const open = !dismissed && suggestions.length > 0;

  const syncCaret = () => {
    const el = ref.current;
    if (el) setCaret(el.selectionStart === el.selectionEnd ? el.selectionStart : null);
  };

  const accept = (s: Suggestion) => {
    if (!token || caret === null) return;
    const next = `${value.slice(0, token.start)}$${s.name}${value.slice(caret)}`;
    const pos = token.start + s.name.length + 1;
    pendingCaret.current = pos;
    onChange(next);
    setCaret(pos);
    ref.current?.focus();
  };

  const onKeyDown = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (open) {
      if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
        e.preventDefault();
        const delta = e.key === 'ArrowDown' ? 1 : -1;
        setActive((a) => (a + delta + suggestions.length) % suggestions.length);
        return;
      }
      if (e.key === 'Enter' || e.key === 'Tab') {
        const pick = suggestions[Math.min(active, suggestions.length - 1)];
        if (pick && !e.ctrlKey) {
          e.preventDefault();
          accept(pick);
          return;
        }
      }
      if (e.key === 'Escape') {
        e.preventDefault();
        e.stopPropagation();
        setDismissed(true);
        return;
      }
    }
    if ((e.ctrlKey || e.metaKey) && (e.key === 'Enter' || e.key.toLowerCase() === 's')) {
      e.preventDefault();
      onSubmit?.();
    }
  };

  return (
    <div className="cmd-input">
      <textarea
        ref={ref}
        className={`field field--mono${invalid ? ' field--invalid' : ''}`}
        value={value}
        rows={Math.min(6, Math.max(2, value.split('\n').length))}
        spellCheck={false}
        autoFocus={autoFocus}
        placeholder='rmdir /s /q "$ruta"'
        role="combobox"
        aria-expanded={open}
        aria-controls={listId}
        aria-autocomplete="list"
        onChange={(e) => {
          onChange(e.target.value);
          setDismissed(false);
          setActive(0);
          setCaret(e.target.selectionStart);
        }}
        onKeyDown={onKeyDown}
        onKeyUp={(e) => {
          if (!['ArrowDown', 'ArrowUp', 'Enter', 'Tab', 'Escape'].includes(e.key)) syncCaret();
        }}
        onClick={syncCaret}
        onBlur={() => setTimeout(() => setCaret(null), 120)}
      />
      {open && (
        <ul className="suggest" id={listId} role="listbox">
          {suggestions.map((s, i) => (
            <li
              key={s.name}
              role="option"
              aria-selected={i === active}
              className={`suggest__item${i === active ? ' is-active' : ''}`}
              onMouseDown={(e) => {
                e.preventDefault(); // no perder el foco del textarea
                accept(s);
              }}
              onMouseEnter={() => setActive(i)}
            >
              <span className="mono">${s.name}</span>
              <span className="suggest__meta">
                {s.sensitive && <span className="badge badge--sensitive">sensible</span>}
                {s.scope}
              </span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
