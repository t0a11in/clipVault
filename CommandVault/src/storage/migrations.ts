import { SCHEMA_VERSION } from '../types/model';
import { DataError } from './validate';

/**
 * Migraciones de esquema.
 *
 * Cada migración transforma datos de la versión N a la N+1 y se registra con
 * la clave N. Para cambiar el formato:
 *   1. Sube SCHEMA_VERSION en types/model.ts.
 *   2. Agrega aquí `[versiónAnterior]: (data) => nuevoFormato`.
 *   3. Agrega un test en migrations.test.ts con un JSON real de la versión anterior.
 * Nunca edites una migración ya publicada: los datos de otros equipos dependen de ella.
 */
export type Migration = (data: Record<string, unknown>) => Record<string, unknown>;
export type MigrationMap = Readonly<Record<number, Migration>>;

export const MIGRATIONS: MigrationMap = {
  /**
   * v0 → v1. "v0" representa datos sin `schemaVersion` (prototipos o archivos
   * editados a mano) donde las entradas venían anidadas dentro de cada tab:
   *   { tabs: [{ id, name, entries: [...] }] }
   * Se aplanan a la lista `entries` con su `tabId`.
   */
  0: (data) => {
    // Sin lista de tabs no hay nada que migrar: se deja tal cual para que la
    // validación lo rechace (evita aceptar un JSON cualquiera como "config vacía").
    if (!Array.isArray(data.tabs)) return { ...data, schemaVersion: 1 };
    const tabs: unknown[] = data.tabs;
    const flatEntries: unknown[] = Array.isArray(data.entries) ? [...data.entries] : [];
    const cleanTabs = tabs.map((tab, tabIndex) => {
      if (typeof tab !== 'object' || tab === null) return tab;
      const { entries, ...rest } = tab as Record<string, unknown>;
      if (Array.isArray(entries)) {
        entries.forEach((entry, i) => {
          if (typeof entry === 'object' && entry !== null) {
            flatEntries.push({ order: i, ...entry, tabId: (rest.id as string) ?? '' });
          }
        });
      }
      return { order: tabIndex, ...rest };
    });
    return { ...data, tabs: cleanTabs, entries: flatEntries, schemaVersion: 1 };
  },
};

/** Lee la versión declarada; datos sin versión se consideran v0. */
export function readVersion(data: Record<string, unknown>): number {
  const v = data.schemaVersion;
  return typeof v === 'number' && Number.isInteger(v) && v >= 0 ? v : 0;
}

/**
 * Aplica en orden las migraciones necesarias hasta `target`.
 * Lanza DataError si los datos son de una versión más nueva que la app
 * (no se pueden "desmigrar" sin perder información) o si falta un paso.
 */
export function migrate(
  data: Record<string, unknown>,
  migrations: MigrationMap = MIGRATIONS,
  target: number = SCHEMA_VERSION,
): Record<string, unknown> {
  let version = readVersion(data);
  if (version > target) {
    throw new DataError(
      `Los datos son de la versión ${version} y esta app solo entiende hasta la ${target}. Actualiza CommandVault.`,
    );
  }
  let current = data;
  while (version < target) {
    const step = migrations[version];
    if (!step) throw new DataError(`No existe migración desde la versión ${version}.`);
    current = step(current);
    version += 1;
    current = { ...current, schemaVersion: version };
  }
  return current;
}
