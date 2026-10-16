# Biblioteca de ClipVault (maqueta funcional)

Vista nativa WPF para guardar y reutilizar mensajes, comandos, prompts y fragmentos.
Convive con el Historial sin modificarlo. **Todo vive en memoria**: nada se guarda en SQLite.

## Estructura

| Carpeta | Qué contiene |
|---|---|
| `ClipVault.Core/Models/Library` | Modelos provisionales: `Section`, `LibraryPage`, `FieldDefinition`, `FieldValue`, `FieldType` |
| `ClipVault.Core/Services/TemplateRenderer.cs` | Lógica pura de plantillas `$archivo` (render y extracción desde el ejemplo) |
| `ClipVault.Core/Interfaces/ILibraryDataSource.cs` | Origen de datos (hoy en memoria; mañana SQLite) |
| `Shell/` | Riel lateral Historial / Biblioteca (`ShellViewModel`) |
| `Themes/` | Tokens del JSON, estilos con clave y alto contraste. **Sin estilos implícitos** |
| `Components/` | Componentes de presentación reutilizables (solo `DependencyProperty`, sin ViewModel propio) |
| `Features/Library/` | `LibraryViewModel`, secciones, páginas y datos de ejemplo |
| `Features/Library/Editor/` | Editor: lectura, plantilla, datos sensibles y edición |
| `Features/Library/Settings/` | Panel ⚙ de copiado y pegado |

## Atajos (con el foco dentro de la Biblioteca)

| Atajo | Acción |
|---|---|
| Ctrl+N | Nueva página en la sección activa |
| Ctrl+Shift+N | Nueva sección |
| Ctrl+F | Buscar páginas |
| F2 | Renombrar la sección activa |
| Ctrl+S | Guardar la edición |
| Esc | Cerrar ⚙ · cancelar edición (pregunta si hay cambios) · cancelar renombrado |
| Enter | Confirmar nombre de sección o página |

## Operaciones simuladas (maqueta)

- Secciones, páginas, campos nuevos, duplicar y eliminar: solo en memoria.
- "Pegar en el campo activo" (⚙): opción visible, todavía sin conexión con el editor.
- Datos sensibles de ejemplo: marcadores `${SECRET:…}`, nunca valores reales.

## Pendientes para la etapa de persistencia

1. **Exclusión en el Historial (requiere autorización):** al copiar un dato sensible desde la
   Biblioteca se marca con `ExcludeClipboardContentFromMonitorProcessing`, pero el monitor de
   ClipVault aún no lee esa marca y guarda la copia en el Historial. Cambio propuesto: 3 líneas en
   `MainWindow.OnClipboardChanged`.
2. Tablas y migración EF Core para `Section`, `LibraryPage`, `FieldDefinition` y `FieldValue`
   (implementar `ILibraryDataSource` en Infrastructure).
3. Cifrado de valores sensibles antes de persistirlos (por ejemplo DPAPI, vinculado al usuario de Windows).
4. Atajos globales de la Biblioteca (hoy funcionan con el foco dentro de la vista).
5. Variables `$texto`, `$ruta`, `$nombre` y `$fecha` (ya previstas en `TemplateRenderer.PlannedVariables`).
6. Verificar con escalado de Windows al 100 %, 125 % y 150 % y con alto contraste activado.

## Pruebas

Proyecto `tests/ClipVault.Tests.Unit` (xUnit). Ejecutar: `dotnet test`.
