using System.Collections.ObjectModel;
using ClipVault.Core.Models.Library;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClipVault.App.Features.Library.Settings;

/// <summary>Qué texto produce "Copiar" en el editor.</summary>
public enum CopyMode
{
    /// <summary>La plantilla tal cual (sin reemplazar $archivo).</summary>
    Original,

    /// <summary>El comando con las variables reemplazadas (predeterminado).</summary>
    Rendered,

    /// <summary>El comando renderizado dentro de un bloque de código Markdown.</summary>
    CodeBlock,

    /// <summary>Solo los campos marcados en la configuración.</summary>
    SelectedFields
}

/// <summary>De dónde sale el valor inicial de $archivo.</summary>
public enum FileVariableSource
{
    /// <summary>Se deduce del campo "Ejemplo" de la página.</summary>
    Example,

    /// <summary>Se elige con el selector de archivo o carpeta (valor de demostración inicial).</summary>
    FilePicker
}

/// <summary>Campo que se puede marcar para el modo "Copiar solo campos seleccionados".</summary>
public sealed partial class CopyFieldOption : ObservableObject
{
    public CopyFieldOption(FieldDefinition definition, bool isSelected)
    {
        Definition = definition;
        this.isSelected = isSelected;
    }

    public FieldDefinition Definition { get; }

    public string Label => Definition.Label;

    [ObservableProperty]
    private bool isSelected;
}

/// <summary>
/// Configuración de copiado y pegado (etapa 9). Maqueta: vive en memoria y se
/// pierde al cerrar la app. Los campos sensibles nunca se ofrecen para copiar en bloque.
/// </summary>
public sealed partial class CopyPasteSettingsViewModel : ObservableObject
{
    public CopyPasteSettingsViewModel(IEnumerable<FieldDefinition> definitions)
    {
        foreach (var definition in definitions.Where(d => !d.IsSensitive && d.Type != FieldType.Secret && d.Type != FieldType.Tags))
        {
            AddFieldOption(definition);
        }
    }

    // ----- Modo de copiado -----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSelectedFieldsMode))]
    private CopyMode copyMode = CopyMode.Rendered;

    public bool IsSelectedFieldsMode => CopyMode == CopyMode.SelectedFields;

    /// <summary>Campos (no sensibles) disponibles para el modo "campos seleccionados".</summary>
    public ObservableCollection<CopyFieldOption> FieldOptions { get; } = [];

    // ----- Modo de pegado -----

    /// <summary>Maqueta: en una etapa posterior, "Pegar" escribirá en el campo con foco.</summary>
    [ObservableProperty]
    private bool pasteIntoActiveField = true;

    /// <summary>Habilita el botón que toma el portapapeles como valor de una variable.</summary>
    [ObservableProperty]
    private bool useClipboardForFileVariable = true;

    /// <summary>Pide confirmación antes de reemplazar un valor de $archivo ya escrito.</summary>
    [ObservableProperty]
    private bool confirmBeforeReplace = true;

    /// <summary>Rechaza pegar textos que parecen secretos (contraseñas, tokens, llaves).</summary>
    [ObservableProperty]
    private bool rejectSensitivePaste = true;

    // ----- Variables -----

    /// <summary>Variables disponibles en las plantillas.</summary>
    public string VariableToken => "$archivo · $texto · $ruta · $nombre · $fecha";

    [ObservableProperty]
    private FileVariableSource fileVariableSource = FileVariableSource.Example;

    /// <summary>Campos que se copian en el modo "campos seleccionados".</summary>
    public IEnumerable<FieldDefinition> SelectedDefinitions =>
        FieldOptions.Where(o => o.IsSelected).Select(o => o.Definition);

    /// <summary>Se avisa cuando cambia cualquier opción (el editor recalcula su vista previa).</summary>
    public event EventHandler? Changed;

    /// <summary>Registra un campo nuevo (por ejemplo, uno creado con "+ Agregar campo").</summary>
    public void AddFieldOption(FieldDefinition definition)
    {
        if (definition.IsSensitive || definition.Type is FieldType.Secret or FieldType.Tags ||
            FieldOptions.Any(o => o.Definition.Id == definition.Id))
        {
            return;
        }

        // Por defecto se marcan descripción y comando.
        var option = new CopyFieldOption(definition, definition.Key is "description" or "command");
        option.PropertyChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
        FieldOptions.Add(option);
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Heurística (no garantía) para no capturar secretos al pegar: detecta patrones
    /// comunes de contraseñas, tokens, llaves privadas y cadenas de conexión.
    /// </summary>
    public static bool LooksSensitive(string text)
    {
        var lower = text.ToLowerInvariant();
        string[] markers =
        [
            "password=", "pwd=", "secret", "token", "apikey", "api_key", "api-key",
            "-----begin", "bearer ", "connectionstring", "accountkey="
        ];
        return markers.Any(lower.Contains);
    }
}
