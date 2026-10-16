import type { Variable } from '../types/model';
import { isValidVariableName, normalizeVariableName } from '../domain/variables';
import { newId } from '../domain/ids';

interface Props {
  variables: Variable[];
  onChange: (variables: Variable[]) => void;
  /** Nombres usados en el comando (para avisar de variables sin uso). */
  usedNames?: string[];
  /** Nombres de variables globales (para avisar cuando una local la reemplaza). */
  globalNames?: string[];
  emptyHint?: string;
}

/** Errores de validación por id de variable. */
export function validateVariables(variables: Variable[]): Record<string, string> {
  const errors: Record<string, string> = {};
  const seen = new Map<string, string>();
  for (const v of variables) {
    if (!v.name) errors[v.id] = 'Falta el nombre';
    else if (!isValidVariableName(v.name)) errors[v.id] = 'Solo letras, números y _ (sin empezar por número)';
    else if (seen.has(v.name)) errors[v.id] = 'Nombre repetido';
    seen.set(v.name, v.id);
  }
  return errors;
}

/**
 * Editor de variables: lista de { nombre, valor, sensible }.
 * Marcar una variable como sensible borra su valor: nunca se guarda.
 */
export function VariableEditor({ variables, onChange, usedNames, globalNames = [], emptyHint }: Props) {
  const errors = validateVariables(variables);
  const update = (id: string, patch: Partial<Variable>) =>
    onChange(
      variables.map((v) => {
        if (v.id !== id) return v;
        const next = { ...v, ...patch };
        if (next.sensitive) delete next.value;
        else if (next.value === undefined) next.value = '';
        return next;
      }),
    );

  return (
    <div className="vars">
      {variables.length === 0 && <p className="hint">{emptyHint ?? 'Sin variables.'}</p>}
      {variables.map((v) => {
        const unused = usedNames && v.name && !usedNames.includes(v.name);
        const overridesGlobal = globalNames.includes(v.name);
        return (
          <div key={v.id} className="vars__row">
            <label className="vars__name">
              <span className="vars__dollar" aria-hidden>
                $
              </span>
              <input
                className={`field field--mono${errors[v.id] ? ' field--invalid' : ''}`}
                value={v.name}
                placeholder="nombre"
                aria-label="Nombre de la variable"
                spellCheck={false}
                onChange={(e) => update(v.id, { name: normalizeVariableName(e.target.value) })}
              />
            </label>
            {v.sensitive ? (
              <div className="vars__secret" title="Los valores sensibles nunca se guardan">
                🔒 Se mantiene como <span className="mono">${v.name || 'nombre'}</span> al copiar
              </div>
            ) : (
              <input
                className="field field--mono vars__value"
                value={v.value ?? ''}
                placeholder="valor"
                aria-label={`Valor de ${v.name || 'la variable'}`}
                spellCheck={false}
                onChange={(e) => update(v.id, { value: e.target.value })}
              />
            )}
            <label className="check" title="Las variables sensibles no se guardan ni se exportan">
              <input
                type="checkbox"
                checked={v.sensitive}
                onChange={(e) => update(v.id, { sensitive: e.target.checked })}
              />
              Sensible
            </label>
            <button
              type="button"
              className="icon-btn"
              aria-label={`Quitar variable ${v.name}`}
              title="Quitar variable"
              onClick={() => onChange(variables.filter((x) => x.id !== v.id))}
            >
              ×
            </button>
            {(errors[v.id] || unused || overridesGlobal) && (
              <div className="vars__note">
                {errors[v.id] && <span className="error">{errors[v.id]}</span>}
                {!errors[v.id] && unused && <span className="hint">No se usa en el comando</span>}
                {!errors[v.id] && overridesGlobal && <span className="hint">Reemplaza a la global ${v.name}</span>}
              </div>
            )}
          </div>
        );
      })}
      <button
        type="button"
        className="btn btn--ghost btn--sm"
        onClick={() => onChange([...variables, { id: newId(), name: '', sensitive: false, value: '' }])}
      >
        + Variable
      </button>
    </div>
  );
}
