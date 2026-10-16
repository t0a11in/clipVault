const dateFmt = new Intl.DateTimeFormat('es', {
  day: '2-digit',
  month: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
});

/** "18/10 13:42" a partir de un ISO 8601. */
export function formatWhen(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : dateFmt.format(d).replace(',', '');
}
