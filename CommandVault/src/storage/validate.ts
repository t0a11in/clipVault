import {
  DEFAULT_SETTINGS,
  SCHEMA_VERSION,
  type Entry,
  type HistoryItem,
  type PersistedConfig,
  type Settings,
  type Tab,
  type Variable,
} from '../types/model';
import { isValidVariableName } from '../domain/variables';
import { stripSensitive } from '../domain/sanitize';
import { newId } from '../domain/ids';

/**
 * Validación de datos que vienen "de fuera" (localStorage o un .json importado).
 * Sin dependencias: guardas de tipo a mano. Política:
 * - Error estructural (no es objeto, faltan listas) → se lanza DataError.
 * - Elemento suelto inválido → se descarta y se anota en `warnings`.
 * - Campo opcional ausente → se rellena con su valor por defecto.
 */
export class DataError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'DataError';
  }
}

type Obj = Record<string, unknown>;
const isObj = (v: unknown): v is Obj => typeof v === 'object' && v !== null && !Array.isArray(v);
const str = (v: unknown, fallback = ''): string => (typeof v === 'string' ? v : fallback);
const num = (v: unknown, fallback: number): number =>
  typeof v === 'number' && Number.isFinite(v) ? v : fallback;

export interface Validated<T> {
  value: T;
  warnings: string[];
}

function toVariable(raw: unknown, warnings: string[], where: string): Variable | null {
  if (!isObj(raw) || !isValidVariableName(str(raw.name))) {
    warnings.push(`Variable inválida descartada en ${where}.`);
    return null;
  }
  const variable: Variable = {
    id: str(raw.id) || newId(),
    name: str(raw.name),
    sensitive: raw.sensitive === true,
    value: str(raw.value),
  };
  // Un archivo manipulado podría traer el valor de una sensible: se descarta.
  return stripSensitive(variable);
}

function toVariables(raw: unknown, warnings: string[], where: string): Variable[] {
  if (!Array.isArray(raw)) return [];
  const seen = new Set<string>();
  const out: Variable[] = [];
  for (const item of raw) {
    const v = toVariable(item, warnings, where);
    if (!v) continue;
    if (seen.has(v.name)) {
      warnings.push(`Variable $${v.name} duplicada en ${where}; se conservó la primera.`);
      continue;
    }
    seen.add(v.name);
    out.push(v);
  }
  return out;
}

function toTab(raw: unknown, index: number, warnings: string[]): Tab | null {
  if (!isObj(raw) || !str(raw.id) || !str(raw.name).trim()) {
    warnings.push(`Tab inválido descartado (posición ${index + 1}).`);
    return null;
  }
  return { id: str(raw.id), name: str(raw.name).trim(), order: num(raw.order, index) };
}

function toEntry(raw: unknown, index: number, tabIds: Set<string>, warnings: string[]): Entry | null {
  if (!isObj(raw) || !str(raw.id) || !tabIds.has(str(raw.tabId))) {
    warnings.push(`Entrada inválida o sin tab descartada (posición ${index + 1}).`);
    return null;
  }
  const now = new Date().toISOString();
  const description = str(raw.description);
  const lastCopiedAt = str(raw.lastCopiedAt);
  return {
    id: str(raw.id),
    tabId: str(raw.tabId),
    order: num(raw.order, index),
    description,
    command: str(raw.command),
    variables: toVariables(raw.variables, warnings, `"${description || 'entrada sin descripción'}"`),
    copyCount: Math.max(0, Math.floor(num(raw.copyCount, 0))),
    ...(lastCopiedAt ? { lastCopiedAt } : {}),
    createdAt: str(raw.createdAt, now),
    updatedAt: str(raw.updatedAt, now),
  };
}

function toSettings(raw: unknown): Settings {
  const s = isObj(raw) ? raw : {};
  const oneOf = <T extends string>(v: unknown, options: readonly T[], fallback: T): T =>
    options.includes(v as T) ? (v as T) : fallback;
  return {
    theme: oneOf(s.theme, ['light', 'dark'] as const, DEFAULT_SETTINGS.theme),
    resolveSensitiveOnCopy: s.resolveSensitiveOnCopy === true,
    copyMode: oneOf(s.copyMode, ['command', 'commandWithComment'] as const, DEFAULT_SETTINGS.copyMode),
    commentStyle: oneOf(s.commentStyle, ['bash', 'cmd', 'slashes'] as const, DEFAULT_SETTINGS.commentStyle),
    // Entre 0,5 s y 30 s; valores absurdos vuelven al predeterminado.
    toastDurationMs: (() => {
      const ms = num(s.toastDurationMs, DEFAULT_SETTINGS.toastDurationMs);
      return ms >= 500 && ms <= 30000 ? ms : DEFAULT_SETTINGS.toastDurationMs;
    })(),
  };
}

/** Valida una configuración ya migrada a la versión actual. */
export function validateConfig(raw: unknown): Validated<PersistedConfig> {
  if (!isObj(raw)) throw new DataError('El contenido no es un objeto de configuración.');
  if (!Array.isArray(raw.tabs) || !Array.isArray(raw.entries)) {
    throw new DataError('Faltan las listas "tabs" o "entries".');
  }
  const warnings: string[] = [];

  const tabs: Tab[] = [];
  const tabIds = new Set<string>();
  raw.tabs.forEach((t, i) => {
    const tab = toTab(t, i, warnings);
    if (tab && !tabIds.has(tab.id)) {
      tabIds.add(tab.id);
      tabs.push(tab);
    }
  });

  const entries: Entry[] = [];
  const entryIds = new Set<string>();
  raw.entries.forEach((e, i) => {
    const entry = toEntry(e, i, tabIds, warnings);
    if (entry && !entryIds.has(entry.id)) {
      entryIds.add(entry.id);
      entries.push(entry);
    }
  });

  return {
    value: {
      schemaVersion: SCHEMA_VERSION,
      tabs: normalizeOrder(tabs),
      entries,
      globalVariables: toVariables(raw.globalVariables, warnings, 'variables globales'),
      settings: toSettings(raw.settings),
    },
    warnings,
  };
}

/** Valida una lista de historial (elementos inválidos se descartan en silencio). */
export function validateHistory(raw: unknown): HistoryItem[] {
  if (!Array.isArray(raw)) return [];
  const out: HistoryItem[] = [];
  for (const item of raw) {
    if (!isObj(item) || !str(item.id) || !str(item.at)) continue;
    const vars = Array.isArray(item.variables) ? item.variables : [];
    out.push({
      id: str(item.id),
      at: str(item.at),
      entryId: str(item.entryId),
      tabId: str(item.tabId),
      entryDescription: str(item.entryDescription),
      tabName: str(item.tabName),
      copiedText: str(item.copiedText),
      replayText: str(item.replayText, str(item.copiedText)),
      variables: vars.filter(isObj).map((v) => ({
        name: str(v.name),
        sensitive: v.sensitive === true,
        // Nunca se acepta un valor sensible desde fuera.
        value: v.sensitive === true ? '****' : str(v.value),
      })),
    });
  }
  return out;
}

/** Reasigna `order` consecutivo (0..n-1) respetando el orden actual. */
export function normalizeOrder<T extends { order: number }>(items: T[]): T[] {
  return [...items].sort((a, b) => a.order - b.order).map((item, i) => ({ ...item, order: i }));
}
