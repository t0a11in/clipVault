import { useMemo, useState } from 'react';
import type { Entry, Tab, Variable } from '../types/model';
import { searchEntries } from '../domain/search';
import { Modal } from './Modal';
import { CommandText } from './CommandText';

interface Props {
  entries: Entry[];
  tabs: Tab[];
  globals: Variable[];
  onClose: () => void;
  onGoTo: (entry: Entry) => void;
  onCopy: (entry: Entry) => void;
}

/** Búsqueda global (Ctrl+F): Enter abre la entrada, Ctrl+Enter la copia directamente. */
export function SearchOverlay({ entries, tabs, globals, onClose, onGoTo, onCopy }: Props) {
  const [query, setQuery] = useState('');
  const [active, setActive] = useState(0);
  const results = useMemo(() => searchEntries(entries, query).slice(0, 30), [entries, query]);
  const tabName = (id: string) => tabs.find((t) => t.id === id)?.name ?? '';
  const current = results[Math.min(active, results.length - 1)];

  return (
    <Modal title="Buscar" onClose={onClose} size="lg" placement="top" hideHeader>
      <div className="search">
        <input
          className="search__input"
          autoFocus
          value={query}
          placeholder="Buscar en descripciones y comandos…"
          aria-label="Buscar"
          spellCheck={false}
          onChange={(e) => {
            setQuery(e.target.value);
            setActive(0);
          }}
          onKeyDown={(e) => {
            if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
              e.preventDefault();
              if (results.length === 0) return;
              const delta = e.key === 'ArrowDown' ? 1 : -1;
              setActive((a) => (a + delta + results.length) % results.length);
            } else if (e.key === 'Enter' && current) {
              e.preventDefault();
              if (e.ctrlKey || e.metaKey || e.shiftKey) onCopy(current);
              else onGoTo(current);
            }
          }}
        />
        {query.trim() !== '' && results.length === 0 && <p className="search__empty muted">Sin resultados para «{query}».</p>}
        {results.length > 0 && (
          <ul className="search__results" role="listbox" aria-label="Resultados">
            {results.map((entry, i) => (
              <li
                key={entry.id}
                role="option"
                aria-selected={i === active}
                className={`search__item${i === active ? ' is-active' : ''}`}
                onMouseEnter={() => setActive(i)}
                onMouseDown={(e) => e.preventDefault()}
                onClick={() => onGoTo(entry)}
              >
                <div className="search__line">
                  <span className="search__desc">{entry.description || '(sin descripción)'}</span>
                  <span className="search__tab">{tabName(entry.tabId)}</span>
                </div>
                <CommandText command={entry.command} variables={entry.variables} globals={globals} mode="template" />
              </li>
            ))}
          </ul>
        )}
        <footer className="search__foot">
          <span>
            <kbd>↑↓</kbd> elegir
          </span>
          <span>
            <kbd>Enter</kbd> abrir
          </span>
          <span>
            <kbd>Ctrl Enter</kbd> copiar
          </span>
          <span>
            <kbd>Esc</kbd> cerrar
          </span>
        </footer>
      </div>
    </Modal>
  );
}
