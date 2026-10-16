import type { CommentStyle, CopyMode, Variable } from '../types/model';
import { VARIABLE_PATTERN } from './variables';

/** Texto con el que se enmascaran las variables sensibles en historial y vistas. */
export const MASK = '****';

export interface ResolveOptions {
  /** Variables de la entrada; tienen prioridad sobre las globales. */
  variables: Variable[];
  /** Variables globales (Ajustes). */
  globals?: Variable[];
  /** Ajuste "Resolver variables sensibles al copiar". */
  resolveSensitive?: boolean;
  /** Valores sensibles introducidos en esta sesión (solo memoria, nunca persistidos). */
  sessionSecrets?: Readonly<Record<string, string>>;
}

export interface ResolveResult {
  /** Texto final para el portapapeles. */
  text: string;
  /** Igual que `text` pero con cada sensible como ****. Seguro para historial. */
  masked: string;
  /** Igual que `text` pero con cada sensible como $nombre. Seguro para recopiar. */
  replay: string;
  /** Variables no sensibles definidas pero sin valor: quedan como $nombre. */
  unresolved: string[];
  /** Variables sensibles encontradas en el comando. */
  sensitive: string[];
  /** Sensibles que quedaron como $nombre en `text` (el usuario debe rellenarlas). */
  sensitivePending: string[];
}

/**
 * Resuelve las variables `$nombre` de una plantilla.
 *
 * Reglas:
 * 1. Se busca primero en las variables de la entrada y después en las globales.
 * 2. Un `$x` que no está definido en ninguna parte se deja intacto. Así no se
 *    rompen variables propias del shell como `$HOME` o `$PATH`.
 * 3. Una variable no sensible con valor vacío se deja como `$nombre` y se
 *    informa en `unresolved`.
 * 4. Una variable sensible se deja como `$nombre`, salvo que `resolveSensitive`
 *    esté activo y exista su valor en `sessionSecrets`.
 * 5. El reemplazo se hace en una sola pasada: si un valor contiene "$algo",
 *    no se vuelve a expandir (evita recursión e inyecciones accidentales).
 */
export function resolveCommand(template: string, options: ResolveOptions): ResolveResult {
  const lookup = new Map<string, Variable>();
  for (const g of options.globals ?? []) lookup.set(g.name, g);
  for (const v of options.variables) lookup.set(v.name, v); // la entrada pisa a la global

  const unresolved = new Set<string>();
  const sensitive = new Set<string>();
  const sensitivePending = new Set<string>();

  // Una sola función calcula las tres variantes para que nunca diverjan.
  const render = (mode: 'text' | 'masked' | 'replay'): string =>
    template.replace(VARIABLE_PATTERN, (whole: string, name: string) => {
      const variable = lookup.get(name);
      if (!variable) return whole; // regla 2

      if (variable.sensitive) {
        sensitive.add(name);
        if (mode === 'masked') return MASK;
        if (mode === 'replay') return whole;
        const secret = options.sessionSecrets?.[name];
        if (options.resolveSensitive && secret !== undefined && secret !== '') return secret;
        sensitivePending.add(name);
        return whole; // regla 4
      }

      const value = variable.value ?? '';
      if (value === '') {
        unresolved.add(name);
        return whole; // regla 3
      }
      return value;
    });

  return {
    text: render('text'),
    masked: render('masked'),
    replay: render('replay'),
    unresolved: [...unresolved],
    sensitive: [...sensitive],
    sensitivePending: [...sensitivePending],
  };
}

/** Segmento de una plantilla, para pintarla con las variables resaltadas. */
export type TemplateSegment =
  | { kind: 'text'; text: string }
  | {
      kind: 'var';
      name: string;
      /** value: tiene valor · empty: definida sin valor · sensitive · unknown: no definida */
      state: 'value' | 'empty' | 'sensitive' | 'unknown';
      value?: string;
      fromGlobal: boolean;
    };

/** Divide una plantilla en texto y variables (misma búsqueda que resolveCommand). */
export function tokenizeTemplate(
  template: string,
  variables: Variable[],
  globals: Variable[] = [],
): TemplateSegment[] {
  const local = new Map(variables.map((v) => [v.name, v]));
  const global = new Map(globals.map((v) => [v.name, v]));
  const segments: TemplateSegment[] = [];
  let last = 0;
  for (const match of template.matchAll(VARIABLE_PATTERN)) {
    const index = match.index ?? 0;
    const name = match[1] ?? '';
    if (index > last) segments.push({ kind: 'text', text: template.slice(last, index) });
    const variable = local.get(name) ?? global.get(name);
    const fromGlobal = !local.has(name) && global.has(name);
    if (!variable) segments.push({ kind: 'var', name, state: 'unknown', fromGlobal });
    else if (variable.sensitive) segments.push({ kind: 'var', name, state: 'sensitive', fromGlobal });
    else if (!variable.value) segments.push({ kind: 'var', name, state: 'empty', fromGlobal });
    else segments.push({ kind: 'var', name, state: 'value', value: variable.value, fromGlobal });
    last = index + match[0].length;
  }
  if (last < template.length) segments.push({ kind: 'text', text: template.slice(last) });
  return segments;
}

const COMMENT_PREFIX: Record<CommentStyle, string> = {
  bash: '#',
  cmd: 'REM',
  slashes: '//',
};

/**
 * Aplica el modo de copia: solo el comando, o comando precedido de la
 * descripción como comentario del shell elegido (una línea por cada línea).
 */
export function formatForCopy(
  command: string,
  description: string,
  mode: CopyMode,
  style: CommentStyle,
): string {
  const desc = description.trim();
  if (mode === 'command' || desc === '') return command;
  const prefix = COMMENT_PREFIX[style];
  const comment = desc
    .split(/\r?\n/)
    .map((line) => `${prefix} ${line}`.trimEnd())
    .join('\n');
  return `${comment}\n${command}`;
}
