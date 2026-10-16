# CommandVault

Gestor personal de comandos frecuentes con **tabs verticales** (estilo OneNote). Está pensado para no volver a escribir comandos con rutas largas: guardas la plantilla una vez, con `$variables`, y la copias resuelta con un clic o un atajo.

![Tab con variables sensibles](docs/screenshots/tab-variables-sensibles.png)

- **Sin backend.** Todo funciona en el navegador y los datos quedan en `localStorage`.
- **React 19 + Vite + TypeScript.** Sin librerías de UI: los estilos van en CSS con variables (tema claro y oscuro) y el drag & drop es nativo de HTML5.
- Pensado para escritorio y preparado para empaquetarse después con **Tauri**.

---

## Puesta en marcha

Requisitos: **Node.js 20 o superior**.

```bash
cd CommandVault
npm install
npm run dev        # http://localhost:5173
```

| Script | Qué hace |
|---|---|
| `npm run dev` | Servidor de desarrollo con recarga en caliente |
| `npm run build` | Revisa los tipos (`tsc`) y genera el build en `dist/` |
| `npm run preview` | Sirve `dist/` para probar el build |
| `npm test` | Pruebas unitarias (Vitest) de la lógica de dominio y la persistencia |
| `npm run typecheck` | Solo la revisión de tipos |

> El portapapeles moderno (`navigator.clipboard`) exige un contexto seguro: `localhost`, `https` o Tauri. Si abres `dist/index.html` directamente con `file://`, la app usa un método alternativo de copia.

---

## Uso

### Tabs
- **Crear:** botón `+` de la barra lateral o `Ctrl+Shift+N`. El nombre queda en modo edición al crearlo.
- **Renombrar:** doble clic, `F2` o el lápiz ✎.
- **Reordenar:** arrastrando, o con `Alt+↑ / Alt+↓` sobre el tab enfocado.
- **Eliminar:** botón × y confirmación. También borra sus entradas; el historial se conserva.
- **Frecuentes** es fijo y siempre va primero.

### Entradas
Cada entrada tiene **descripción**, **comando** (la plantilla), un **ejemplo** (el resultado ya resuelto, calculado al vuelo) y sus **variables**.

| Descripción | Comando | Variables |
|---|---|---|
| Eliminar node_modules | `rmdir /s /q "$ruta"` | `ruta = D:\2026\proyectos\SnapGasto\frontend\angular\node_modules` |
| Repopack de un proyecto | `repopack "$origen" --output "$destino"` | `origen`, `destino` |
| Conectar a PostgreSQL | `psql "postgresql://$DB_USER:$DB_PASSWORD@localhost/$DB_NAME"` | `DB_PASSWORD` 🔒 sensible |

![Editor de entrada](docs/screenshots/editor-variables.png)

- `Ctrl+N` crea una entrada en el tab activo. `Ctrl+Enter` o `Ctrl+S` guarda y `Esc` cancela. Si cancelas una entrada nueva que dejaste vacía, se descarta.
- Al escribir `$` en el comando aparecen sugerencias con las variables de la entrada y las globales.
- Al guardar, las `$variables` nuevas que encuentre en el comando se agregan solas. Si el nombre parece de un secreto (`*PASSWORD*`, `*TOKEN*`, `*SECRET*`, `API_KEY`…), se proponen ya marcadas como sensibles.
- Desde el editor puedes mover la entrada a otro tab.

### Sistema de variables

| Caso | Qué se copia |
|---|---|
| Variable con valor | El valor |
| Variable definida pero vacía | `$nombre` tal cual, y un aviso para rellenarla |
| Variable **sensible** | `$nombre` tal cual (o el valor de la sesión, si activaste "Resolver sensibles") |
| `$x` no definida en ningún lado (`$HOME`, `$PATH`…) | Tal cual, para no romper las variables del shell |

- **Prioridad:** si hay una variable de la entrada y una global con el mismo nombre, gana la de la entrada.
- **Coincidencia exacta:** `$ruta` nunca reemplaza parte de `$ruta2`.
- **Una sola pasada:** si un valor contiene `$algo`, no se vuelve a expandir.
- Las **variables globales** se administran en Ajustes (por ejemplo `proyectos = D:\2026\proyectos`).

### Privacidad de las variables sensibles
- Se guardan **solo con su nombre y el flag `sensitive: true`**, nunca con su valor. Esto se comprueba al guardar, al exportar y al importar (`domain/sanitize.ts`), así que un archivo manipulado tampoco puede meter un valor.
- Con **"Resolver variables sensibles al copiar"** activo (por defecto está apagado), al copiar se piden los valores. Quedan **solo en memoria** hasta cerrar o recargar la app; no van a `localStorage`, al historial ni a los exports.
- En el historial las sensibles aparecen como `****`. Si vuelves a copiar una entrada desde el historial, se pegan como `$nombre`.
- Recuerda que el comando ya resuelto queda en el portapapeles del sistema, y cualquier gestor de portapapeles puede verlo.

### Frecuentes e historial

![Frecuentes con el detalle de una copia](docs/screenshots/frecuentes-historial.png)

- **Más usados:** las 5 entradas con más copias.
- **Historial:** las últimas 20 copias, con fecha, entrada y tab. Se guardan hasta 200.
- Un clic en una copia del historial la **vuelve a copiar** y abre el panel lateral, donde puedes ver qué valores tenían las variables en ese momento.

### Búsqueda

![Búsqueda](docs/screenshots/busqueda.png)

`Ctrl+F` busca en descripciones y comandos de todos los tabs, sin distinguir tildes ni mayúsculas. `Enter` abre la entrada y `Ctrl+Enter` la copia directamente.

### Ajustes
- **Qué copiar:** solo el comando, o el comando con la descripción como comentario (`#` bash/PowerShell, `REM` cmd, `//` otros).
- **Duración de la notificación** (por defecto 2 s).
- **Tema** claro u oscuro, que también se cambia desde la barra lateral.
- **Exportar e importar** `.json`, con el historial como opción. Antes de reemplazar tus datos, la importación muestra un resumen.
- **Limpiar historial.** Los contadores de Frecuentes no se tocan.

![Tema oscuro](docs/screenshots/tema-oscuro.png)

---

## Atajos de teclado

| Atajo | Variante en el navegador | Acción |
|---|---|---|
| `Ctrl+1 … 9` | `Alt+1 … 9` | Ir al tab 1º … 9º (sin contar Frecuentes) |
| `Ctrl+0` | `Alt+0` | Ir a Frecuentes |
| `Ctrl+N` | `Alt+N` | Nueva entrada en el tab activo |
| `Ctrl+Shift+N` | `Alt+Shift+N` | Nuevo tab |
| `Ctrl+F` | | Buscar |
| `Ctrl+Shift+C` | `Alt+Shift+C` | Copiar la entrada seleccionada |
| `Ctrl+Enter` | | Copiar sin notificación, o cerrar la que esté abierta |
| `↑ / ↓` | | Moverse entre entradas |
| `Enter` / `E` | | Copiar o editar la entrada enfocada |
| `Tab / Shift+Tab` | | Cambiar de tab (solo fuera de campos de texto) |

**¿Por qué hay variantes con Alt?** Chrome y Edge se reservan `Ctrl+N`, `Ctrl+Shift+N` y `Ctrl+1…9` y no se los pasan a la página, así que en el navegador hay que usar la variante con Alt. Empaquetada como app de escritorio, ambas variantes funcionan. Además, ningún atajo de una página web funciona con la ventana en segundo plano: para un `Ctrl+Shift+C` **global de verdad** hace falta Tauri (ver abajo).

---

## Arquitectura

```text
CommandVault/
├── index.html · vite.config.ts · tsconfig.json · package.json
├── docs/screenshots/          # capturas de referencia
└── src/
    ├── main.tsx · App.tsx     # arranque · orquestación de vistas, diálogos y atajos
    ├── types/model.ts         # interfaces del modelo + SCHEMA_VERSION
    ├── domain/                # lógica pura, sin React → se puede testear
    │   ├── resolveCommand.ts  # resolveCommand, tokenizeTemplate, formatForCopy
    │   ├── variables.ts       # sintaxis $var, detección y autocompletado
    │   ├── sanitize.ts        # quita los valores sensibles
    │   ├── frequent.ts · search.ts · format.ts · ids.ts
    ├── storage/
    │   ├── storage.ts         # localStorage: carga, guardado y respaldo de datos corruptos
    │   ├── migrations.ts      # migraciones de esquema vN → vN+1
    │   ├── validate.ts        # validación de datos externos
    │   ├── exportImport.ts    # exportar e importar .json
    │   └── seed.ts            # 3 ejemplos iniciales
    ├── state/store.tsx        # reductor puro + Context (sin librerías de estado)
    ├── hooks/                 # useKeybindings, useCopyEntry, useClipboard, useTheme
    ├── components/            # Sidebar, TabPanel, EntryCard, VariableEditor, CommandInput,
    │                          # FrequentTab, HistoryInspector, SearchOverlay, SettingsModal,
    │                          # SecretPrompt, Modal, Toast, CommandText
    └── styles/index.css       # tokens de color, temas y componentes
```

**Flujo de una copia** (`hooks/useCopyEntry.ts`):

1. `resolveCommand` genera tres textos en una sola pasada: `text` (lo que se copia), `masked` (con `****`, para el historial) y `replay` (con `$nombre`, para volver a copiar).
2. `formatForCopy` agrega el comentario si así está configurado.
3. Se escribe en el portapapeles.
4. Se registra con `copy/record`, que suma el contador y agrega la copia al historial.

### Persistencia y migraciones
- Se usan dos claves: `commandvault:config` (lo que se exporta) y `commandvault:history`.
- Al cargar se siguen tres pasos: `JSON.parse`, luego `migrate()` y luego `validateConfig()`. Si los datos están ilegibles, se guarda una copia en `commandvault:corrupt:*` y la app arranca con los ejemplos, sin borrar nada en silencio.
- **Para cambiar el formato:** sube `SCHEMA_VERSION` en `types/model.ts`, agrega la migración `[versiónAnterior]: (data) => …` en `storage/migrations.ts` y escribe su test con un JSON real. No edites migraciones que ya se publicaron.
- Si un archivo es de una versión **más nueva** que la app, se rechaza en lugar de arriesgar perder datos.

---

## Empaquetar como app de escritorio (Tauri)

Es el paso siguiente. Necesita [Rust](https://www.rust-lang.org/tools/install) y las dependencias de Tauri para Windows (WebView2 ya viene en Windows 10 y 11).

```bash
npm install -D @tauri-apps/cli
npx tauri init        # dist: ../dist · dev: http://localhost:5173
npx tauri dev
```

Pendientes de esa fase:
- **Atajo global real:** con `@tauri-apps/plugin-global-shortcut`, registrar `Ctrl+Shift+C` para copiar la entrada seleccionada aunque la ventana esté oculta.
- **Exportar:** en Tauri, `a[download]` no abre un diálogo. Conviene usar `@tauri-apps/plugin-dialog` + `plugin-fs`.
- Si algún día los datos crecen mucho, se puede pasar de `localStorage` (unos 5 MB) a IndexedDB o a un archivo JSON en la carpeta de datos de la app. Solo hay que cambiar `storage.ts`.

---

## Decisiones y límites conocidos
- Los estilos van en **CSS con variables** y no en Tailwind. Así no se agregan dependencias y los temas quedan en tokens. Si prefieres Tailwind, migrar es mecánico porque las clases ya están organizadas por componente.
- **Ajustes** se abre como un modal desde la barra lateral, no como un tab más.
- Los valores sensibles de la sesión se guardan **por nombre**: si dos entradas usan `$DB_PASSWORD`, comparten el valor durante la sesión.
- Recopiar desde el historial también cuenta como uso, porque la especificación pide registrar cada copia.
