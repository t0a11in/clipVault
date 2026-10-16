/** Identificador único. `crypto.randomUUID` existe en contextos seguros
 *  (https, localhost, Tauri); el respaldo cubre file:// en navegadores antiguos. */
export function newId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  return `id-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
}

/** Marca de tiempo ISO 8601. Inyectable en pruebas. */
export function nowIso(): string {
  return new Date().toISOString();
}
