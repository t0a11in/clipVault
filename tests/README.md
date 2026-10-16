# Pruebas de ClipVault

| Proyecto | Tipo | Herramientas |
|---|---|---|
| `tests/ClipVault.Tests.Unit` | Unitarias (sin UI, sin portapapeles real, sin BD) | xUnit + **Moq** |
| `tests/ClipVault.Tests.Integration` | Integración con **SQLite real** en archivo temporal | xUnit + EF Core Sqlite |

```bash
dotnet test                                          # todo
dotnet test tests/ClipVault.Tests.Unit               # solo unitarias
dotnet test --filter "FullyQualifiedName~Library"    # solo Biblioteca
```

## Convenciones

- Nombre: `Método_Escenario_ResultadoEsperado`. Cuerpo: **Arrange → Act → Assert**.
- `[Fact]` = un caso; `[Theory]` + `[InlineData]` = mismo caso con varios datos.
- xUnit crea una instancia de la clase por prueba: el constructor es el "Arrange" común.
- Moq: `Setup(...).Returns(...)` define respuestas; `Verify(..., Times.Once)` comprueba llamadas.
- Datos de prueba propios (no se usan los ejemplos de la app, salvo en la prueba que los protege).

## Cobertura

| Archivo | Qué protege |
|---|---|
| `Library/PageListItemViewModelMatchesTests` | Búsqueda: título/contenido, tildes y mayúsculas; **nunca encuentra por campos sensibles** |
| `Library/PageListItemViewModelPreviewTests` | Vista previa: comando primero, primera línea; **nunca muestra secretos** |
| `Library/InlineRenameViewModelTests` | Renombrar: Enter/Esc/perder foco, nombre vacío |
| `Library/LibraryViewModelTests` (Moq) | Navegación, contadores, búsqueda que conserva selección, nueva página, copiar (texto exacto), **secretos nunca copiados sin confirmar**, eliminar con confirmación, bloqueo al editar |
| `Library/SampleLibraryDataSourceTests` | Los datos de ejemplo solo tienen marcadores `${SECRET:…}` |
| `Core/TemplateRendererTests` | `$archivo`: reemplazo, variables ajenas intactas, una sola pasada, deducir desde "Ejemplo" |
| `History/MainViewModelTests` (Moq) | Historial existente: carga, captura, guardar permanente, eliminar seleccionado/marcados, **no borra guardados desde Recientes** |
| `Integration/ClipRepositoryTests` | SQLite real: captura siempre temporal, **imagen como BLOB**, guardado permanente y categoría, orden y filtro, eliminar |
| `Integration/DatabaseInitializerTests` | Inicializar dos veces no pierde datos |

## Pendiente

- La limpieza por retención del historial no existe aún en el repositorio; cuando exista,
  agregar la prueba "**la limpieza nunca elimina permanentes**".
- Pruebas de UI (FlaUI) y del monitor/atajos Win32: manuales por ahora.
