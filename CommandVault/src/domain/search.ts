import type { Entry } from '../types/model';

/** Minúsculas y sin tildes, para que "configuración" encuentre "configuracion". */
export function normalizeForSearch(text: string): string {
  return text.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
}

/**
 * Busca en descripción y comando. Todas las palabras de la consulta deben
 * aparecer (en cualquier orden). Las coincidencias en la descripción puntúan más.
 */
export function searchEntries(entries: Entry[], query: string): Entry[] {
  const terms = normalizeForSearch(query).split(/\s+/).filter(Boolean);
  if (terms.length === 0) return [];

  const scored: { entry: Entry; score: number }[] = [];
  for (const entry of entries) {
    const desc = normalizeForSearch(entry.description);
    const cmd = normalizeForSearch(entry.command);
    let score = 0;
    let all = true;
    for (const term of terms) {
      const inDesc = desc.includes(term);
      const inCmd = cmd.includes(term);
      if (!inDesc && !inCmd) {
        all = false;
        break;
      }
      score += (inDesc ? 2 : 0) + (inCmd ? 1 : 0);
    }
    if (all) scored.push({ entry, score: score + Math.min(entry.copyCount, 50) / 100 });
  }
  return scored.sort((a, b) => b.score - a.score).map((s) => s.entry);
}
