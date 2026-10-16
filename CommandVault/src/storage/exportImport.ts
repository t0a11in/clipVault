import { SCHEMA_VERSION, type ExportFile, type HistoryItem, type PersistedConfig } from '../types/model';
import { sanitizeConfig } from '../domain/sanitize';
import { migrate } from './migrations';
import { DataError, validateConfig, validateHistory } from './validate';

/**
 * Construye el archivo de exportación. Los valores sensibles se eliminan
 * (se conservan nombre y flag). El historial solo se incluye si se pide.
 */
export function buildExport(
  config: PersistedConfig,
  history: HistoryItem[] | null,
  now: string = new Date().toISOString(),
): ExportFile {
  return {
    app: 'CommandVault',
    exportedAt: now,
    schemaVersion: SCHEMA_VERSION,
    config: sanitizeConfig(config),
    ...(history ? { history } : {}),
  };
}

/** Nombre sugerido: commandvault-2026-10-16.json */
export function exportFileName(now: Date = new Date()): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `commandvault-${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}.json`;
}

export interface ImportResult {
  config: PersistedConfig;
  history: HistoryItem[] | null;
  warnings: string[];
}

/**
 * Lee un .json exportado (o el JSON crudo de la configuración).
 * Migra si viene de una versión anterior y valida todo antes de aceptarlo.
 * Lanza DataError con un mensaje para el usuario si no es válido.
 */
export function parseImport(text: string): ImportResult {
  let raw: unknown;
  try {
    raw = JSON.parse(text);
  } catch {
    throw new DataError('El archivo no es un JSON válido.');
  }
  if (typeof raw !== 'object' || raw === null || Array.isArray(raw)) {
    throw new DataError('El archivo no tiene el formato de CommandVault.');
  }
  const file = raw as Record<string, unknown>;

  // Acepta tanto el envoltorio de exportación como una configuración "suelta".
  const isWrapped = file.app === 'CommandVault' && typeof file.config === 'object';
  const configRaw = (isWrapped ? file.config : file) as Record<string, unknown>;

  const { value, warnings } = validateConfig(migrate(configRaw));
  const history = isWrapped && Array.isArray(file.history) ? validateHistory(file.history) : null;
  return { config: value, history, warnings };
}
