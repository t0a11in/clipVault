/**
 * Modelo de datos de CommandVault.
 *
 * Regla de oro de privacidad: una variable con `sensitive = true` NUNCA lleva
 * `value` al almacenamiento ni a una exportación. Las funciones de
 * `domain/sanitize.ts` lo hacen cumplir en cada punto de salida.
 */

/** Versión actual del esquema persistido. Súbela al cambiar la forma del JSON
 *  y agrega la migración correspondiente en `storage/migrations.ts`. */
export const SCHEMA_VERSION = 1;

/** Variable de una entrada (o global). El nombre se guarda sin "$". */
export interface Variable {
  id: string;
  /** Nombre sin "$": [A-Za-z_][A-Za-z0-9_]* */
  name: string;
  sensitive: boolean;
  /** Valor guardado. Siempre ausente cuando `sensitive` es true. */
  value?: string;
}

/** Sección vertical de la barra lateral ("Frontend", "Docker", ...). */
export interface Tab {
  id: string;
  name: string;
  /** Posición en la barra lateral (0 = primero tras "Frecuentes"). */
  order: number;
}

/** Comando guardado dentro de un tab. */
export interface Entry {
  id: string;
  tabId: string;
  /** Posición dentro del tab. */
  order: number;
  description: string;
  /** Plantilla con variables $nombre. */
  command: string;
  variables: Variable[];
  /** Veces copiada; alimenta el top de "Frecuentes". */
  copyCount: number;
  /** ISO 8601 de la última copia. */
  lastCopiedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export type ThemeMode = 'light' | 'dark';
export type CopyMode = 'command' | 'commandWithComment';
/** bash → "#", cmd → "REM", slashes → "//" */
export type CommentStyle = 'bash' | 'cmd' | 'slashes';

/** Configuración global (vista "Ajustes"). */
export interface Settings {
  theme: ThemeMode;
  /** Si es true, al copiar se piden los valores sensibles (solo en memoria). */
  resolveSensitiveOnCopy: boolean;
  copyMode: CopyMode;
  commentStyle: CommentStyle;
  /** Duración de la notificación de copia, en milisegundos. */
  toastDurationMs: number;
}

export const DEFAULT_SETTINGS: Settings = {
  theme: 'light',
  resolveSensitiveOnCopy: false,
  copyMode: 'command',
  commentStyle: 'bash',
  toastDurationMs: 2000,
};

/** Instantánea de una variable en el momento de copiar (sensibles = "****"). */
export interface HistoryVariable {
  name: string;
  value: string;
  sensitive: boolean;
}

/** Registro de una copia. Nunca contiene valores sensibles. */
export interface HistoryItem {
  id: string;
  /** ISO 8601 */
  at: string;
  entryId: string;
  tabId: string;
  /** Copias de nombre por si la entrada o el tab se eliminan después. */
  entryDescription: string;
  tabName: string;
  /** Texto copiado con las sensibles enmascaradas como **** (para mostrar). */
  copiedText: string;
  /** Texto con las sensibles como $nombre (para volver a copiar). */
  replayText: string;
  variables: HistoryVariable[];
}

/** Lo que se guarda en localStorage y lo que se exporta/importa. */
export interface PersistedConfig {
  schemaVersion: number;
  tabs: Tab[];
  entries: Entry[];
  /** Variables disponibles para todas las entradas (y para el autocompletado). */
  globalVariables: Variable[];
  settings: Settings;
}

/** El historial se guarda aparte para poder exportar solo la configuración. */
export interface PersistedHistory {
  schemaVersion: number;
  items: HistoryItem[];
}

/** Formato del archivo .json exportado. */
export interface ExportFile {
  app: 'CommandVault';
  exportedAt: string;
  schemaVersion: number;
  config: PersistedConfig;
  history?: HistoryItem[];
}

/** Máximo de registros de historial persistidos (se muestran los últimos 20). */
export const HISTORY_LIMIT = 200;
export const HISTORY_VISIBLE = 20;
export const FREQUENT_TOP = 5;
