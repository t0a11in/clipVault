import type { Entry, PersistedConfig, Variable } from '../types/model';

/** Quita el valor de una variable sensible. Se aplica en cada salida a disco/archivo. */
export function stripSensitive(variable: Variable): Variable {
  if (!variable.sensitive) return variable;
  const { value: _discarded, ...rest } = variable;
  return rest;
}

function stripEntry(entry: Entry): Entry {
  return { ...entry, variables: entry.variables.map(stripSensitive) };
}

/**
 * Devuelve una copia de la configuración sin ningún valor sensible.
 * Se usa al guardar en localStorage y al exportar (defensa en profundidad:
 * aunque la UI nunca guarda esos valores, aquí se garantiza).
 */
export function sanitizeConfig(config: PersistedConfig): PersistedConfig {
  return {
    ...config,
    entries: config.entries.map(stripEntry),
    globalVariables: config.globalVariables.map(stripSensitive),
  };
}
