/** Colores de las pestañas (como las secciones de OneNote). Se elige uno estable por id. */
const COLORS = ['violet', 'teal', 'amber', 'rose', 'blue', 'green', 'orange', 'slate'] as const;
export type TabColor = (typeof COLORS)[number];

export function tabColor(id: string): TabColor {
  let hash = 0;
  for (let i = 0; i < id.length; i++) hash = (hash * 31 + id.charCodeAt(i)) | 0;
  return COLORS[Math.abs(hash) % COLORS.length] ?? 'violet';
}
