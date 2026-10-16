using ClipVault.Core.Interfaces;
using ClipVault.Core.Models.Library;

namespace ClipVault.App.Features.Library.Data;

/// <summary>
/// Datos de ejemplo en memoria para la maqueta. Nada se guarda en SQLite.
/// Los datos sensibles son marcadores ("${SECRET:…}"), nunca valores reales.
/// </summary>
public sealed class SampleLibraryDataSource : ILibraryDataSource
{
    private readonly List<Section> _sections;
    private readonly List<FieldDefinition> _fields;
    private readonly List<LibraryPage> _pages;

    public SampleLibraryDataSource()
    {
        var messages = new Section { Name = "Mensajes", ColorHex = "#2563EB", IconKey = "chat", Order = 0 };
        var commands = new Section { Name = "Comandos", ColorHex = "#15803D", IconKey = "terminal", Order = 1 };
        var prompts = new Section { Name = "Prompts", ColorHex = "#DB2777", IconKey = "document", Order = 2 };
        var snippets = new Section { Name = "Fragmentos", ColorHex = "#B45309", IconKey = "code", Order = 3 };
        _sections = [messages, commands, prompts, snippets];

        var description = new FieldDefinition { Key = "description", Label = "Descripción", Type = FieldType.MultilineText, DisplayOrder = 0, IsCopySource = false };
        var command = new FieldDefinition { Key = "command", Label = "Comando", Type = FieldType.Code, DisplayOrder = 1, IsCopySource = true, IsCommandTemplate = true };
        var example = new FieldDefinition { Key = "example", Label = "Ejemplo", Type = FieldType.Text, DisplayOrder = 2 };
        var secret = new FieldDefinition { Key = "secret", Label = "Token de API", Type = FieldType.Secret, DisplayOrder = 3, IsSensitive = true };
        var tags = new FieldDefinition { Key = "tags", Label = "Etiquetas", Type = FieldType.Tags, DisplayOrder = 4 };
        _fields = [description, command, example, secret, tags];

        LibraryPage Page(Section section, string title, DateTime updatedAt, params (FieldDefinition Field, string Value)[] values) => new()
        {
            SectionId = section.Id,
            Title = title,
            CreatedAt = updatedAt.AddDays(-3),
            UpdatedAt = updatedAt,
            IsSensitive = values.Any(v => v.Field.IsSensitive),
            Fields = values.Select(v => new FieldValue { FieldDefinitionId = v.Field.Id, Value = v.Value }).ToList()
        };

        _pages =
        [
            Page(messages, "Saludo a cliente", new DateTime(2026, 10, 9, 10, 15, 0),
                (description, "Hola, gracias por escribirnos. Revisaremos tu solicitud y te responderemos hoy.")),
            Page(messages, "Seguimiento de reunión", new DateTime(2026, 10, 13, 17, 40, 0),
                (description, "Te comparto el resumen de la reunión y los próximos pasos acordados.")),

            Page(commands, "Dockerfile", new DateTime(2026, 10, 14, 9, 5, 0),
                (description, "Construir la imagen de la API."),
                (command, "docker build -t api ."),
                (tags, "docker, api")),
            Page(commands, "Angular", new DateTime(2026, 10, 12, 18, 20, 0),
                (description, "Compilar el frontend para producción."),
                (command, "ng build --configuration production")),
            Page(commands, "CMD", new DateTime(2026, 10, 10, 11, 0, 0),
                (description, "Definir la clave de la API en la sesión actual."),
                (command, "set API_KEY=$API_KEY"),
                (secret, "${SECRET:API_KEY}")),
            Page(commands, "IA Clonar", new DateTime(2026, 10, 16, 15, 42, 0),
                (description, "Llevar árbol del proyecto de una IA a otra."),
                (command, "repopack \"$archivo\""),
                (example, "repopack \"D:\\2026\\proyectos\\SnapGasto\\\""),
                (tags, "ia, repopack")),

            // Ejemplo con varias variables: $nombre, $texto y $fecha se deducen del campo Ejemplo.
            Page(commands, "Git: rama y primer commit", new DateTime(2026, 10, 15, 12, 0, 0),
                (description, "Crear una rama y registrar el primer commit con fecha."),
                (command, "git checkout -b \"$nombre\" && git commit -m \"$texto ($fecha)\""),
                (example, "git checkout -b \"feature/login\" && git commit -m \"Inicio del login (2026-10-15)\""),
                (tags, "git")),

            Page(prompts, "Revisión de código", new DateTime(2026, 10, 15, 8, 30, 0),
                (description, "Revisa este código y señala errores, riesgos y mejoras con ejemplos.")),
            Page(prompts, "Resumen técnico", new DateTime(2026, 10, 11, 14, 10, 0),
                (description, "Resume el siguiente texto técnico en cinco puntos para un equipo no técnico.")),

            Page(snippets, "Backup previo a saltar versión", new DateTime(2026, 10, 8, 19, 0, 0),
                (description, "Respaldar archivos de configuración antes de actualizar Angular."),
                (command,
                    "$backup = Join-Path $PWD (\"angular21-backup-\" + (Get-Date -Format yyyyMMdd-HHmmss))\n" +
                    "New-Item -ItemType Directory -Path $backup\n" +
                    "Copy-Item package.json, package-lock.json, angular.json, tsconfig.spec.json -Destination $backup\n" +
                    "Set-Location $backup"))
        ];

        _pages.Single(p => p.Title == "IA Clonar").IsFavorite = true;
        _pages.Single(p => p.Title == "Revisión de código").IsFavorite = true;
    }

    public IReadOnlyList<Section> GetSections() => _sections;

    public IReadOnlyList<FieldDefinition> GetFieldDefinitions() => _fields;

    public IReadOnlyList<LibraryPage> GetPages() => _pages;
}
