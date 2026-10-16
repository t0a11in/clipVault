import type { Variable } from '../types/model';
import { newId } from './ids';

/**
 * Sintaxis de variables: `$nombre`, donde nombre = [A-Za-z_][A-Za-z0-9_]*.
 * La coincidencia es "codiciosa": en `$ruta2` el nombre es `ruta2`, nunca `ruta`.
 */
export const VARIABLE_PATTERN = /\$([A-Za-z_][A-Za-z0-9_]*)/g;
const VALID_NAME = /^[A-Za-z_][A-Za-z0-9_]*$/;

/** Valida un nombre de variable (sin "$"). */
export function isValidVariableName(name: string): boolean {
  return VALID_NAME.test(name);
}

/** Normaliza lo que escribe el usuario: quita "$" inicial y espacios. */
export function normalizeVariableName(raw: string): string {
  return raw.trim().replace(/^\$+/, '');
}

/** Nombres de variables usados en un comando, sin repetir y en orden de aparición. */
export function extractVariableNames(command: string): string[] {
  const seen = new Set<string>();
  for (const match of command.matchAll(VARIABLE_PATTERN)) {
    const name = match[1];
    if (name) seen.add(name);
  }
  return [...seen];
}

/**
 * Agrega a `variables` los nombres que aparecen en el comando y no están
 * definidos ni en la entrada ni como globales. No elimina nada: una variable
 * sin uso puede ser intencional y borrarla perdería su valor.
 */
export function addMissingVariables(
  command: string,
  variables: Variable[],
  globals: Variable[] = [],
): Variable[] {
  const known = new Set([...variables, ...globals].map((v) => v.name));
  const missing = extractVariableNames(command).filter((n) => !known.has(n));
  if (missing.length === 0) return variables;
  return [
    ...variables,
    ...missing.map((name) => ({ id: newId(), name, sensitive: looksSensitive(name), value: '' })),
  ];
}

/**
 * Sugerencia (no garantía) de que un nombre suele contener un secreto.
 * Solo se usa para marcar por defecto el check "sensible" al detectar variables;
 * el usuario decide siempre.
 */
export function looksSensitive(name: string): boolean {
  return /pass(word)?|pwd|secret|token|api_?key|private|credential/i.test(name);
}

/**
 * Busca la palabra `$parcial` que termina en la posición del cursor.
 * Devuelve el prefijo escrito (sin "$") y dónde empieza el "$", o null.
 */
export function variableTokenAt(
  text: string,
  caret: number,
): { prefix: string; start: number } | null {
  const before = text.slice(0, caret);
  const match = /\$([A-Za-z0-9_]*)$/.exec(before);
  if (!match) return null;
  return { prefix: match[1] ?? '', start: caret - match[0].length };
}
