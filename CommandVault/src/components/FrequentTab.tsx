import type { Entry, HistoryItem, Tab } from '../types/model';
import { HISTORY_VISIBLE } from '../types/model';
import { topFrequent } from '../domain/frequent';
import { useEffect, useRef } from 'react';
import { formatWhen } from '../domain/format';
import { HistoryInspector } from './HistoryInspector';

interface Props {
  entries: Entry[];
  tabs: Tab[];
  history: HistoryItem[];
  selectedId: string | null;
  inspectedId: string | null;
  onSelect: (id: string) => void;
  onCopy: (entry: Entry) => void;
  onReplay: (item: HistoryItem) => void;
  onCloseInspector: () => void;
  onGoToEntry: (entry: Entry) => void;
}

/** Tab fijo "Frecuentes": top 5 por número de copias e historial reciente. */
export function FrequentTab(props: Props) {
  const top = topFrequent(props.entries);
  const recent = props.history.slice(0, HISTORY_VISIBLE);
  const tabName = (id: string) => props.tabs.find((t) => t.id === id)?.name ?? '';
  const inspected = props.history.find((h) => h.id === props.inspectedId) ?? null;

  return (
    <div className={`frequent${inspected ? ' has-inspector' : ''}`}>
      <section className="panel" aria-label="Frecuentes">
        <header className="panel__head">
          <div>
            <h2 className="panel__title">Frecuentes</h2>
            <p className="panel__sub">Tus 5 comandos más copiados y las últimas {HISTORY_VISIBLE} copias</p>
          </div>
        </header>

        <div className="block">
          <h3 className="block__title">
            <span aria-hidden>★</span> Más usados
          </h3>
          {top.length === 0 ? (
            <p className="muted block__empty">Aún no has copiado nada. Cada copia suma aquí.</p>
          ) : (
            <ol className="rank">
              {top.map((entry, i) => (
                <RankItem
                  key={entry.id}
                  entry={entry}
                  position={i + 1}
                  tabName={tabName(entry.tabId)}
                  selected={props.selectedId === entry.id}
                  onSelect={() => props.onSelect(entry.id)}
                  onCopy={() => props.onCopy(entry)}
                  onGoTo={() => props.onGoToEntry(entry)}
                />
              ))}
            </ol>
          )}
        </div>

        <div className="block">
          <h3 className="block__title">
            <span aria-hidden>↺</span> Historial
          </h3>
          {recent.length === 0 ? (
            <p className="muted block__empty">El historial está vacío.</p>
          ) : (
            <ul className="history">
              {recent.map((item) => (
                <li key={item.id}>
                  <button
                    type="button"
                    className={`history__item${props.inspectedId === item.id ? ' is-active' : ''}`}
                    onClick={() => props.onReplay(item)}
                    title="Copiar de nuevo e inspeccionar"
                  >
                    <time className="history__when" dateTime={item.at}>
                      {formatWhen(item.at)}
                    </time>
                    <span className="history__desc">{item.entryDescription || '(sin descripción)'}</span>
                    <span className="history__tab">{item.tabName}</span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>

      {inspected && <HistoryInspector item={inspected} onClose={props.onCloseInspector} onReplay={props.onReplay} />}
    </div>
  );
}

interface RankProps {
  entry: Entry;
  position: number;
  tabName: string;
  selected: boolean;
  onSelect: () => void;
  onCopy: () => void;
  onGoTo: () => void;
}

/** Fila del top. Se enfoca al seleccionarla con ↑↓ para que Enter la copie. */
function RankItem({ entry, position, tabName, selected, onSelect, onCopy, onGoTo }: RankProps) {
  const ref = useRef<HTMLLIElement>(null);
  useEffect(() => {
    const el = ref.current;
    if (selected && el && !el.contains(document.activeElement)) el.focus({ preventScroll: true });
  }, [selected]);
  return (
    <li
      ref={ref}
      className={`rank__item${selected ? ' is-selected' : ''}`}
      data-nav-zone="list"
      tabIndex={0}
      onFocus={(e) => {
        if (e.target === e.currentTarget) onSelect();
      }}
      onMouseDown={onSelect}
      onKeyDown={(e) => {
        if (e.key === 'Enter' && e.target === e.currentTarget) onCopy();
      }}
    >
      <span className="rank__pos">{position}</span>
      <button type="button" className="rank__main" onClick={onGoTo} title="Ir a la entrada">
        <span className="rank__desc">{entry.description || entry.command}</span>
        <span className="rank__tab">{tabName}</span>
      </button>
      <span className="count">{entry.copyCount}×</span>
      <button type="button" className="btn btn--primary btn--sm" onClick={onCopy}>
        Copiar
      </button>
    </li>
  );
}
