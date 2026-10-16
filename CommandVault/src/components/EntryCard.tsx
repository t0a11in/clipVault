import { useEffect, useRef, useState, type KeyboardEvent } from 'react';
import type { Entry, Tab, Variable } from '../types/model';
import { addMissingVariables, extractVariableNames } from '../domain/variables';
import { CommandText } from './CommandText';
import { CommandInput } from './CommandInput';
import { VariableEditor, validateVariables } from './VariableEditor';

interface Props {
  entry: Entry;
  globals: Variable[];
  tabs: Tab[];
  selected: boolean;
  editing: boolean;
  onSelect: () => void;
  onCopy: () => void;
  onEdit: () => void;
  onCancelEdit: () => void;
  onSave: (patch: Pick<Entry, 'description' | 'command' | 'variables' | 'tabId'>) => void;
  onDelete: () => void;
}

/** Tarjeta de una entrada: vista de lectura (descripción, comando, ejemplo, variables) o edición. */
export function EntryCard(props: Props) {
  const { entry, selected, editing } = props;
  const ref = useRef<HTMLElement>(null);

  // La selección por teclado mantiene la tarjeta visible y con foco.
  useEffect(() => {
    if (selected && !editing && ref.current) {
      ref.current.scrollIntoView({ block: 'nearest' });
      if (!ref.current.contains(document.activeElement)) ref.current.focus({ preventScroll: true });
    }
  }, [selected, editing]);

  return (
    <article
      ref={ref}
      className={`entry${selected ? ' is-selected' : ''}${editing ? ' is-editing' : ''}`}
      data-nav-zone={editing ? undefined : 'list'}
      tabIndex={editing ? -1 : 0}
      aria-label={entry.description || 'Entrada sin descripción'}
      onMouseDown={props.onSelect}
      onFocus={(e) => {
        if (e.target === e.currentTarget) props.onSelect();
      }}
      onKeyDown={(e) => {
        // Atajos de la tarjeta enfocada: Enter copia, E edita.
        if (editing || e.target !== e.currentTarget || e.ctrlKey || e.metaKey || e.altKey) return;
        if (e.key === 'Enter') {
          e.preventDefault();
          props.onCopy();
        } else if (e.key.toLowerCase() === 'e') {
          e.preventDefault();
          props.onEdit();
        }
      }}
    >
      {editing ? <EntryEditor {...props} /> : <EntryView {...props} />}
    </article>
  );
}

function EntryView({ entry, globals, onCopy, onEdit }: Props) {
  const names = extractVariableNames(entry.command);
  const known = new Map([...globals, ...entry.variables].map((v) => [v.name, v]));
  const usedVars = names.map((n) => known.get(n)).filter((v): v is Variable => v !== undefined);
  const localOnly = entry.variables.filter((v) => !names.includes(v.name));
  const hasVars = names.some((n) => known.has(n));

  return (
    <div className="entry__body" onDoubleClick={onEdit}>
      <header className="entry__head">
        <h3 className="entry__desc">{entry.description || <em className="muted">Sin descripción</em>}</h3>
        <div className="entry__actions">
          {entry.copyCount > 0 && (
            <span className="count" title="Veces copiada">
              {entry.copyCount}×
            </span>
          )}
          <button type="button" className="btn btn--ghost btn--sm" onClick={onEdit} title="Editar (E)">
            Editar
          </button>
          <button
            type="button"
            className="btn btn--primary btn--sm"
            onClick={(e) => {
              e.stopPropagation();
              onCopy();
            }}
            title="Copiar (Enter · Ctrl+Shift+C)"
          >
            Copiar
          </button>
        </div>
      </header>

      <dl className="entry__grid">
        <dt>Comando</dt>
        <dd>
          <CommandText command={entry.command} variables={entry.variables} globals={globals} mode="template" />
        </dd>
        {hasVars && (
          <>
            <dt>Ejemplo</dt>
            <dd>
              <CommandText command={entry.command} variables={entry.variables} globals={globals} mode="example" />
            </dd>
          </>
        )}
        {(usedVars.length > 0 || localOnly.length > 0) && (
          <>
            <dt>Variables</dt>
            <dd className="chips">
              {[...usedVars, ...localOnly].map((v) => {
                const isGlobal = !entry.variables.some((x) => x.name === v.name);
                return (
                  <span key={v.name} className={`chip${v.sensitive ? ' chip--sensitive' : ''}`}>
                    <span className="chip__name">{v.name}</span>
                    {v.sensitive ? (
                      <span className="chip__value">🔒 sensible</span>
                    ) : (
                      <span className="chip__value" title={v.value}>
                        {v.value || <em>sin valor</em>}
                      </span>
                    )}
                    {isGlobal && <span className="chip__scope">global</span>}
                  </span>
                );
              })}
            </dd>
          </>
        )}
      </dl>
    </div>
  );
}

function EntryEditor({ entry, globals, tabs, onSave, onCancelEdit, onDelete }: Props) {
  const [description, setDescription] = useState(entry.description);
  const [command, setCommand] = useState(entry.command);
  const [variables, setVariables] = useState<Variable[]>(entry.variables);
  const [tabId, setTabId] = useState(entry.tabId);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [tried, setTried] = useState(false);

  const varErrors = validateVariables(variables);
  const commandError = command.trim() === '' ? 'El comando no puede estar vacío' : null;
  const valid = !commandError && Object.keys(varErrors).length === 0;
  const used = extractVariableNames(command);
  const globalNames = globals.map((g) => g.name);
  const missing = used.filter((n) => !variables.some((v) => v.name === n) && !globalNames.includes(n));

  const save = () => {
    setTried(true);
    if (!valid) return;
    onSave({
      description: description.trim(),
      command,
      // Las variables detectadas en el comando se agregan automáticamente.
      variables: addMissingVariables(command, variables, globals),
      tabId,
    });
  };

  const onKeyDown = (e: KeyboardEvent) => {
    if (e.key === 'Escape') {
      e.preventDefault();
      e.stopPropagation();
      onCancelEdit();
    } else if ((e.ctrlKey || e.metaKey) && (e.key === 'Enter' || e.key.toLowerCase() === 's')) {
      e.preventDefault();
      e.stopPropagation();
      save();
    }
  };

  return (
    <form
      className="editor"
      onKeyDown={onKeyDown}
      onSubmit={(e) => {
        e.preventDefault();
        save();
      }}
    >
      <label className="editor__field">
        <span className="label">Descripción</span>
        <input
          className="field"
          value={description}
          placeholder="Eliminar node_modules"
          autoFocus={entry.description === ''}
          onChange={(e) => setDescription(e.target.value)}
        />
      </label>

      <div className="editor__field">
        <span className="label">Comando</span>
        <CommandInput
          value={command}
          onChange={setCommand}
          variables={variables}
          globals={globals}
          autoFocus={entry.description !== ''}
          onSubmit={save}
          invalid={tried && !!commandError}
        />
        {tried && commandError && <span className="error">{commandError}</span>}
        <span className="hint">
          Usa <span className="mono">$nombre</span> para variables. Escribe <span className="mono">$</span> para ver
          sugerencias. Un <span className="mono">$x</span> no definido se copia tal cual.
        </span>
      </div>

      <div className="editor__field">
        <div className="label-row">
          <span className="label">Variables</span>
          {missing.length > 0 && (
            <button
              type="button"
              className="btn btn--ghost btn--sm"
              onClick={() => setVariables(addMissingVariables(command, variables, globals))}
            >
              Agregar detectadas: {missing.map((m) => `$${m}`).join(', ')}
            </button>
          )}
        </div>
        <VariableEditor
          variables={variables}
          onChange={setVariables}
          usedNames={used}
          globalNames={globalNames}
          emptyHint="Sin variables propias. Las que escribas en el comando se agregan al guardar."
        />
      </div>

      <footer className="editor__foot">
        <label className="editor__tab">
          <span className="label">Tab</span>
          <select className="field" value={tabId} onChange={(e) => setTabId(e.target.value)}>
            {tabs.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
              </option>
            ))}
          </select>
        </label>
        <div className="spacer" />
        {confirmDelete ? (
          <span className="confirm">
            ¿Eliminar esta entrada?
            <button type="button" className="btn btn--danger btn--sm" onClick={onDelete}>
              Sí, eliminar
            </button>
            <button type="button" className="btn btn--ghost btn--sm" onClick={() => setConfirmDelete(false)}>
              No
            </button>
          </span>
        ) : (
          <button type="button" className="btn btn--ghost btn--sm btn--danger-text" onClick={() => setConfirmDelete(true)}>
            Eliminar
          </button>
        )}
        <button type="button" className="btn btn--ghost" onClick={onCancelEdit}>
          Cancelar
        </button>
        <button type="submit" className="btn btn--primary" title="Ctrl+Enter">
          Guardar
        </button>
      </footer>
    </form>
  );
}
