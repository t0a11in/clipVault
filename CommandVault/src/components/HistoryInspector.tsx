import type { HistoryItem } from '../types/model';
import { formatWhen } from '../domain/format';

interface Props {
  item: HistoryItem;
  onClose: () => void;
  onReplay: (item: HistoryItem) => void;
}

/** Panel lateral para inspeccionar una copia del historial. */
export function HistoryInspector({ item, onClose, onReplay }: Props) {
  return (
    <aside className="inspector" aria-label="Detalle de la copia">
      <header className="inspector__head">
        <h3>Detalle de la copia</h3>
        <button type="button" className="icon-btn" onClick={onClose} aria-label="Cerrar detalle">
          ×
        </button>
      </header>
      <dl className="inspector__meta">
        <dt>Entrada</dt>
        <dd>{item.entryDescription || '(sin descripción)'}</dd>
        <dt>Tab</dt>
        <dd>{item.tabName || '—'}</dd>
        <dt>Fecha</dt>
        <dd>{formatWhen(item.at)}</dd>
      </dl>
      <span className="label">Comando copiado</span>
      <pre className="inspector__cmd">{item.copiedText}</pre>
      <span className="label">Variables</span>
      {item.variables.length === 0 ? (
        <p className="muted">Sin variables.</p>
      ) : (
        <table className="inspector__vars">
          <tbody>
            {item.variables.map((v) => (
              <tr key={v.name}>
                <th className="mono">${v.name}</th>
                <td className={`mono${v.sensitive ? ' is-sensitive' : ''}`}>
                  {v.sensitive ? '**** (sensible)' : v.value || <em>sin valor</em>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      <p className="hint">Al volver a copiar, las variables sensibles se pegan como $nombre para que las rellenes.</p>
      <button type="button" className="btn btn--primary" onClick={() => onReplay(item)}>
        Copiar de nuevo
      </button>
    </aside>
  );
}
