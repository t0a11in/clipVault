import type { Entry, Tab, Variable } from '../types/model';
import { EntryCard } from './EntryCard';
import { tabColor } from './tabColor';

interface Props {
  tab: Tab;
  tabIndex: number;
  entries: Entry[];
  tabs: Tab[];
  globals: Variable[];
  selectedId: string | null;
  editingId: string | null;
  onSelect: (id: string) => void;
  onCopy: (entry: Entry) => void;
  onEdit: (id: string) => void;
  onCancelEdit: (entry: Entry) => void;
  onSave: (id: string, patch: Pick<Entry, 'description' | 'command' | 'variables' | 'tabId'>) => void;
  onDelete: (id: string) => void;
  onNewEntry: () => void;
}

/** Contenido de un tab: cabecera y lista de entradas. */
export function TabPanel(props: Props) {
  const { tab, entries } = props;
  return (
    <section className={`panel tone-${tabColor(tab.id)}`} aria-label={`Tab ${tab.name}`}>
      <header className="panel__head">
        <div>
          <h2 className="panel__title">{tab.name}</h2>
          <p className="panel__sub">
            {entries.length === 1 ? '1 entrada' : `${entries.length} entradas`}
            {props.tabIndex < 9 && <span className="kbd-hint"> · Ctrl+{props.tabIndex + 1}</span>}
          </p>
        </div>
        <button type="button" className="btn btn--primary" onClick={props.onNewEntry} title="Ctrl+N (Alt+N en el navegador)">
          + Nueva entrada
        </button>
      </header>

      {entries.length === 0 ? (
        <div className="empty">
          <p className="empty__title">Este tab está vacío</p>
          <p className="muted">
            Guarda aquí los comandos que repites. Usa <span className="mono">$variables</span> para las rutas largas.
          </p>
          <button type="button" className="btn btn--ghost" onClick={props.onNewEntry}>
            Crear la primera entrada
          </button>
        </div>
      ) : (
        <div className="entries" role="list">
          {entries.map((entry) => (
            <EntryCard
              key={entry.id}
              entry={entry}
              globals={props.globals}
              tabs={props.tabs}
              selected={props.selectedId === entry.id}
              editing={props.editingId === entry.id}
              onSelect={() => props.onSelect(entry.id)}
              onCopy={() => props.onCopy(entry)}
              onEdit={() => props.onEdit(entry.id)}
              onCancelEdit={() => props.onCancelEdit(entry)}
              onSave={(patch) => props.onSave(entry.id, patch)}
              onDelete={() => props.onDelete(entry.id)}
            />
          ))}
        </div>
      )}
    </section>
  );
}
