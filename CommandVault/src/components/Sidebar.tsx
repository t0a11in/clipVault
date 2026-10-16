import { useEffect, useRef, useState, type DragEvent, type KeyboardEvent } from 'react';
import type { Entry, Tab, ThemeMode } from '../types/model';
import { tabColor } from './tabColor';

export type View = { type: 'frequent' } | { type: 'tab'; tabId: string };

interface Props {
  tabs: Tab[];
  entries: Entry[];
  view: View;
  theme: ThemeMode;
  /** Id del tab que debe entrar en modo renombrar (recién creado). */
  renamingId: string | null;
  onRenamingDone: () => void;
  onSelectView: (view: View) => void;
  onAddTab: () => void;
  onRenameTab: (id: string, name: string) => void;
  onDeleteTab: (id: string) => void;
  onMoveTab: (id: string, targetId: string, place: 'before' | 'after') => void;
  onOpenSettings: () => void;
  onOpenSearch: () => void;
  onToggleTheme: () => void;
}

/**
 * Columna izquierda: "Frecuentes" fijo arriba, tabs reordenables
 * (arrastrar y soltar, o Alt+↑/↓ con el tab enfocado) y accesos a Ajustes.
 */
export function Sidebar(props: Props) {
  const { tabs, entries, view } = props;
  const [drag, setDrag] = useState<{ id: string; overId: string | null; place: 'before' | 'after' } | null>(null);
  const [confirmId, setConfirmId] = useState<string | null>(null);
  const [renamingId, setRenamingId] = useState<string | null>(null);

  useEffect(() => {
    if (props.renamingId) setRenamingId(props.renamingId);
  }, [props.renamingId]);

  const counts = new Map<string, number>();
  for (const e of entries) counts.set(e.tabId, (counts.get(e.tabId) ?? 0) + 1);

  const onDragOver = (e: DragEvent, tab: Tab) => {
    if (!drag) return;
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    const rect = e.currentTarget.getBoundingClientRect();
    const place = e.clientY < rect.top + rect.height / 2 ? 'before' : 'after';
    if (drag.overId !== tab.id || drag.place !== place) setDrag({ ...drag, overId: tab.id, place });
  };

  const onDrop = (e: DragEvent) => {
    e.preventDefault();
    if (drag?.overId) props.onMoveTab(drag.id, drag.overId, drag.place);
    setDrag(null);
  };

  const onTabKeyDown = (e: KeyboardEvent, index: number, tab: Tab) => {
    if (e.altKey && (e.key === 'ArrowUp' || e.key === 'ArrowDown')) {
      e.preventDefault();
      const target = tabs[e.key === 'ArrowUp' ? index - 1 : index + 1];
      if (target) props.onMoveTab(tab.id, target.id, e.key === 'ArrowUp' ? 'before' : 'after');
    } else if (e.key === 'F2') {
      e.preventDefault();
      setRenamingId(tab.id);
    }
  };

  return (
    <nav className="sidebar" aria-label="Tabs">
      <div className="brand">
        <span className="brand__mark" aria-hidden>
          ›_
        </span>
        <span className="brand__name">CommandVault</span>
      </div>

      <button type="button" className="search-trigger" onClick={props.onOpenSearch}>
        <span>Buscar…</span>
        <kbd>Ctrl F</kbd>
      </button>

      <button
        type="button"
        className={`tab tab--fixed${view.type === 'frequent' ? ' is-active' : ''}`}
        aria-current={view.type === 'frequent' ? 'page' : undefined}
        onClick={() => props.onSelectView({ type: 'frequent' })}
      >
        <span className="tab__icon" aria-hidden>
          ★
        </span>
        <span className="tab__name">Frecuentes</span>
      </button>

      <div className="sidebar__label">
        <span>Tabs</span>
        <button type="button" className="icon-btn" onClick={props.onAddTab} title="Nuevo tab (Ctrl+Shift+N)" aria-label="Nuevo tab">
          +
        </button>
      </div>

      <ul className="tabs" onDragEnd={() => setDrag(null)}>
        {tabs.map((tab, index) => {
          const active = view.type === 'tab' && view.tabId === tab.id;
          const dropClass =
            drag?.overId === tab.id && drag.id !== tab.id ? ` drop-${drag.place}` : '';
          return (
            <li
              key={tab.id}
              className={`tabs__item tone-${tabColor(tab.id)}${dropClass}${drag?.id === tab.id ? ' is-dragging' : ''}`}
              draggable={renamingId !== tab.id}
              onDragStart={(e) => {
                e.dataTransfer.effectAllowed = 'move';
                e.dataTransfer.setData('text/plain', tab.id);
                setDrag({ id: tab.id, overId: null, place: 'before' });
              }}
              onDragOver={(e) => onDragOver(e, tab)}
              onDrop={onDrop}
            >
              {renamingId === tab.id ? (
                <RenameInput
                  initial={tab.name}
                  onDone={(name) => {
                    if (name !== null && name.trim()) props.onRenameTab(tab.id, name.trim());
                    setRenamingId(null);
                    props.onRenamingDone();
                  }}
                />
              ) : confirmId === tab.id ? (
                <div className="tab tab--confirm" role="alert">
                  <span className="tab__confirm-text">
                    ¿Eliminar «{tab.name}»{counts.get(tab.id) ? ` y sus ${counts.get(tab.id)} entradas` : ''}?
                  </span>
                  <span className="tab__confirm-actions">
                    <button
                      type="button"
                      className="btn btn--danger btn--xs"
                      onClick={() => {
                        props.onDeleteTab(tab.id);
                        setConfirmId(null);
                      }}
                    >
                      Eliminar
                    </button>
                    <button type="button" className="btn btn--ghost btn--xs" autoFocus onClick={() => setConfirmId(null)}>
                      Cancelar
                    </button>
                  </span>
                </div>
              ) : (
                <div className={`tab${active ? ' is-active' : ''}`}>
                  <button
                    type="button"
                    className="tab__main"
                    aria-current={active ? 'page' : undefined}
                    title="Arrastra para reordenar · Doble clic o F2 para renombrar"
                    onClick={() => props.onSelectView({ type: 'tab', tabId: tab.id })}
                    onDoubleClick={() => setRenamingId(tab.id)}
                    onKeyDown={(e) => onTabKeyDown(e, index, tab)}
                  >
                    <span className="tab__swatch" aria-hidden />
                    <span className="tab__name">{tab.name}</span>
                    <span className="tab__count" title={index < 9 ? `Ctrl+${index + 1}` : undefined}>
                      {counts.get(tab.id) ?? 0}
                    </span>
                  </button>
                  <span className="tab__tools">
                    <button type="button" className="icon-btn icon-btn--sm" title="Renombrar (F2)" aria-label={`Renombrar ${tab.name}`} onClick={() => setRenamingId(tab.id)}>
                      ✎
                    </button>
                    <button type="button" className="icon-btn icon-btn--sm" title="Eliminar tab" aria-label={`Eliminar ${tab.name}`} onClick={() => setConfirmId(tab.id)}>
                      ×
                    </button>
                  </span>
                </div>
              )}
            </li>
          );
        })}
      </ul>

      {tabs.length === 0 && <p className="sidebar__empty">Crea tu primer tab con «+».</p>}

      <div className="sidebar__foot">
        <button type="button" className="foot-btn" onClick={props.onOpenSettings}>
          ⚙ Ajustes
        </button>
        <button
          type="button"
          className="foot-btn"
          onClick={props.onToggleTheme}
          title="Cambiar tema"
          aria-label={props.theme === 'light' ? 'Activar tema oscuro' : 'Activar tema claro'}
        >
          {props.theme === 'light' ? '☾ Oscuro' : '☀ Claro'}
        </button>
      </div>
    </nav>
  );
}

/** Campo de renombrado en línea: Enter guarda, Esc cancela, perder foco guarda. */
function RenameInput({ initial, onDone }: { initial: string; onDone: (name: string | null) => void }) {
  const [value, setValue] = useState(initial);
  const ref = useRef<HTMLInputElement>(null);
  const done = useRef(false);
  useEffect(() => {
    ref.current?.focus();
    ref.current?.select();
  }, []);
  const finish = (name: string | null) => {
    if (done.current) return;
    done.current = true;
    onDone(name);
  };
  return (
    <input
      ref={ref}
      className="field tab-rename"
      value={value}
      maxLength={40}
      aria-label="Nombre del tab"
      onChange={(e) => setValue(e.target.value)}
      onKeyDown={(e) => {
        if (e.key === 'Enter') finish(value);
        if (e.key === 'Escape') {
          e.stopPropagation();
          finish(null);
        }
      }}
      onBlur={() => finish(value)}
    />
  );
}
