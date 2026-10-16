import {
  HISTORY_LIMIT,
  SCHEMA_VERSION,
  type HistoryItem,
  type PersistedConfig,
  type PersistedHistory,
} from '../types/model';
import { sanitizeConfig } from '../domain/sanitize';
import { migrate } from './migrations';
import { createSeed } from './seed';
import { DataError, validateConfig, validateHistory } from './validate';

/** Claves de localStorage. Config e historial van separados a propósito. */
export const STORAGE_KEYS = {
  config: 'commandvault:config',
  history: 'commandvault:history',
  /** Prefijo de respaldos de datos ilegibles (nunca se borran en silencio). */
  corruptPrefix: 'commandvault:corrupt:',
} as const;

/** Subconjunto de la API Storage: permite inyectar un almacén falso en pruebas. */
export type KeyValueStore = Pick<Storage, 'getItem' | 'setItem'>;

export interface LoadResult {
  config: PersistedConfig;
  history: HistoryItem[];
  /** Avisos para mostrar al usuario (datos reparados, respaldo creado, etc.). */
  warnings: string[];
  /** true si no había datos y se cargaron los ejemplos. */
  seeded: boolean;
}

/** localStorage puede lanzar excepción (modo privado, cuota, políticas). */
export function getBrowserStore(): KeyValueStore | null {
  try {
    const store = window.localStorage;
    const probe = '__commandvault_probe__';
    store.setItem(probe, '1');
    store.removeItem(probe);
    return store;
  } catch {
    return null;
  }
}

function backupCorrupt(store: KeyValueStore, key: string, raw: string): string {
  const backupKey = `${STORAGE_KEYS.corruptPrefix}${key}:${Date.now()}`;
  try {
    store.setItem(backupKey, raw);
    return backupKey;
  } catch {
    return '(no se pudo respaldar: almacenamiento lleno)';
  }
}

/**
 * Carga, migra y valida. Nunca pierde datos en silencio: si algo es ilegible,
 * guarda una copia bajo `commandvault:corrupt:*` y arranca con los ejemplos.
 */
export function loadState(store: KeyValueStore | null): LoadResult {
  const warnings: string[] = [];
  if (!store) {
    return {
      config: createSeed(),
      history: [],
      warnings: ['El almacenamiento local no está disponible: los cambios no se guardarán.'],
      seeded: true,
    };
  }

  let config: PersistedConfig | null = null;
  let seeded = false;
  const rawConfig = store.getItem(STORAGE_KEYS.config);
  if (rawConfig === null) {
    config = createSeed();
    seeded = true;
  } else {
    try {
      const migrated = migrate(JSON.parse(rawConfig) as Record<string, unknown>);
      const result = validateConfig(migrated);
      config = result.value;
      warnings.push(...result.warnings);
    } catch (error) {
      const backupKey = backupCorrupt(store, STORAGE_KEYS.config, rawConfig);
      const reason = error instanceof DataError ? error.message : 'JSON ilegible.';
      warnings.push(`No se pudo leer la configuración guardada (${reason}). Copia de respaldo: ${backupKey}`);
      config = createSeed();
      seeded = true;
    }
  }

  let history: HistoryItem[] = [];
  const rawHistory = store.getItem(STORAGE_KEYS.history);
  if (rawHistory !== null) {
    try {
      const parsed = migrate(JSON.parse(rawHistory) as Record<string, unknown>, {
        // El historial no existía en v0: un historial sin versión se lee tal cual.
        0: (data) => ({ ...data, schemaVersion: 1 }),
      });
      history = validateHistory(parsed.items);
    } catch {
      backupCorrupt(store, STORAGE_KEYS.history, rawHistory);
      warnings.push('El historial guardado era ilegible y se reinició (se guardó una copia).');
    }
  }

  return { config, history, warnings, seeded };
}

/** Guarda la configuración, siempre sin valores sensibles. Devuelve un error legible o null. */
export function saveConfig(store: KeyValueStore | null, config: PersistedConfig): string | null {
  if (!store) return null;
  try {
    store.setItem(STORAGE_KEYS.config, JSON.stringify(sanitizeConfig(config)));
    return null;
  } catch {
    return 'No se pudo guardar: el almacenamiento local está lleno o bloqueado.';
  }
}

export function saveHistory(store: KeyValueStore | null, items: HistoryItem[]): string | null {
  if (!store) return null;
  const payload: PersistedHistory = { schemaVersion: SCHEMA_VERSION, items: items.slice(0, HISTORY_LIMIT) };
  try {
    store.setItem(STORAGE_KEYS.history, JSON.stringify(payload));
    return null;
  } catch {
    return 'No se pudo guardar el historial: almacenamiento lleno.';
  }
}
