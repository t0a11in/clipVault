# CommandVault — instrucciones para asistentes de IA

Este subproyecto es **independiente de ClipVault**. Las reglas del `CLAUDE.md` de la raíz que hablan de C#, WPF, EF Core, SQLite y Win32 **no aplican aquí**. Sí aplican el protocolo de trabajo (inspeccionar → proponer el cambio mínimo → implementar → probar → informar sin inventar verificaciones) y las prioridades: preservar lo que ya funciona, privacidad y simplicidad.

## Stack
React 19 + Vite + TypeScript estricto. Estilos en CSS con variables (`src/styles/index.css`). Pruebas con Vitest. Sin backend; persistencia en `localStorage`. No agregues dependencias sin justificarlo.

## Reglas que no se rompen
- Los **valores de variables sensibles nunca se persisten, exportan ni registran**. Toda salida a disco o archivo pasa por `domain/sanitize.ts`, y el historial guarda `****` (o `$nombre` en `replayText`).
- La lógica va en `domain/` y `storage/` como **funciones puras con tests**. Los componentes solo presentan y despachan acciones al reductor (`state/store.tsx`).
- Para cambiar el formato persistido: sube `SCHEMA_VERSION` y agrega una migración con su test. Nunca edites una migración ya publicada.
- Los comentarios y los textos de la UI van en español.

## Verificación
`npm test` y `npm run build` (incluye `tsc`) antes de dar algo por terminado.
