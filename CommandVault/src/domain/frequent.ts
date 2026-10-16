import type { Entry } from '../types/model';
import { FREQUENT_TOP } from '../types/model';

/**
 * Top de entradas más copiadas. Desempate: la copiada más recientemente primero,
 * y después la descripción (orden estable y predecible).
 */
export function topFrequent(entries: Entry[], limit = FREQUENT_TOP): Entry[] {
  return entries
    .filter((e) => e.copyCount > 0)
    .sort(
      (a, b) =>
        b.copyCount - a.copyCount ||
        (b.lastCopiedAt ?? '').localeCompare(a.lastCopiedAt ?? '') ||
        a.description.localeCompare(b.description, 'es'),
    )
    .slice(0, limit);
}
