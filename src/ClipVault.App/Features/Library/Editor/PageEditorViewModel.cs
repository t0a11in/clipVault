using System.Collections.ObjectModel;
using System.Globalization;
using ClipVault.App.Features.Library.Settings;
using ClipVault.App.Services;
using ClipVault.Core.Models.Library;
using ClipVault.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClipVault.App.Features.Library.Editor;

/// <summary>
/// Editor de la página seleccionada.
/// Etapa 5: modo lectura (encabezado, acciones y campos dinámicos).
/// Etapa 6: plantilla con variables ($archivo, $texto, $ruta, $nombre, $fecha): valores
/// de demostración, selector de archivo/carpeta, pegar y vista previa del comando renderizado.
/// Etapa 7: datos sensibles (mostrar temporalmente, copiar con confirmación).
/// Etapa 8: modo edición (título, campos, "+ Agregar campo", Guardar/Cancelar).
/// Etapa 9: respeta la configuración de copiado y pegado (<see cref="Settings"/>).
/// No ejecuta comandos: solo copia texto cuando el usuario lo pide.
/// "Copiar" de la página nunca incluye datos sensibles; se copian uno a uno y con confirmación.
/// </summary>
public sealed partial class PageEditorViewModel : ObservableObject
{
    /// <summary>Valor de demostración para $archivo (maqueta).</summary>
    public const string DemoFileValue = @"D:\2026\proyectos\SnapGasto\";

    private static readonly CultureInfo DateCulture = CultureInfo.GetCultureInfo("es-CL");

    private readonly List<FieldDefinition> _definitions;
    private readonly IClipboardService _clipboard;
    private readonly IFileDialogService _fileDialogs;
    private readonly IConfirmationService _confirmation;
    private readonly Action<PageListItemViewModel> _duplicate;
    private readonly Action<PageListItemViewModel> _delete;
    private readonly Action<PageListItemViewModel> _saved;

    // Valores de variables por página y nombre, solo en memoria: al volver a una página se conservan.
    private readonly Dictionary<(Guid PageId, string Name), string> _variableValues = [];

    public PageEditorViewModel(
        List<FieldDefinition> definitions,
        IClipboardService clipboard,
        IFileDialogService fileDialogs,
        IConfirmationService confirmation,
        NoticeViewModel notice,
        CopyPasteSettingsViewModel settings,
        Action<PageListItemViewModel> duplicate,
        Action<PageListItemViewModel> delete,
        Action<PageListItemViewModel> saved)
    {
        _definitions = definitions;
        _clipboard = clipboard;
        _fileDialogs = fileDialogs;
        _confirmation = confirmation;
        Notice = notice;
        Settings = settings;
        Settings.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(PreviewCopyText));
            PasteVariableCommand.NotifyCanExecuteChanged();
        };
        _duplicate = duplicate;
        _delete = delete;
        _saved = saved;
    }

    public NoticeViewModel Notice { get; }

    /// <summary>Configuración de copiado y pegado compartida con el panel ⚙.</summary>
    public CopyPasteSettingsViewModel Settings { get; }

    /// <summary>Campos con contenido, en el orden de su definición (sin las etiquetas).</summary>
    public ObservableCollection<FieldViewModel> Fields { get; } = [];

    /// <summary>Etiquetas de la página: se muestran en los metadatos, no como campo.</summary>
    public ObservableCollection<string> Tags { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPage))]
    [NotifyPropertyChangedFor(nameof(CanUsePageActions), nameof(IsReading))]
    [NotifyCanExecuteChangedFor(nameof(CopyCommand), nameof(EditCommand), nameof(DuplicateCommand),
        nameof(DeleteCommand), nameof(ToggleFavoriteCommand))]
    private PageListItemViewModel? page;

    public bool HasPage => Page is not null;

    /// <summary>Las acciones de lectura se desactivan mientras se edita.</summary>
    public bool CanUsePageActions => HasPage && !IsEditing;

    public string TypeLabel => Page?.HasCommand == true ? "Comando" : "Texto";

    public string UpdatedAtText => Page is null
        ? string.Empty
        : "Editado " + Page.Model.UpdatedAt.ToString("dd-MM-yyyy HH:mm", DateCulture);

    public string CharacterCountText
    {
        get
        {
            var count = Fields.Sum(f => f.CharacterCount);
            return count == 1 ? "1 carácter" : $"{count.ToString("N0", DateCulture)} caracteres";
        }
    }

    public bool HasSensitiveFields => Fields.Any(f => f.IsSensitive);

    public bool HasTags => Tags.Count > 0;

    // ===================== Plantilla (etapa 6) =====================

    /// <summary>Plantilla guardada, por ejemplo <c>repopack "$archivo"</c>.</summary>
    [ObservableProperty]
    private string commandTemplate = string.Empty;

    /// <summary>Se muestra solo si la plantilla usa alguna variable implementada.</summary>
    [ObservableProperty]
    private bool hasTemplatePreview;

    /// <summary>
    /// Variables implementadas ($archivo, $texto, $ruta, $nombre, $fecha) que usa la
    /// plantilla, en orden de aparición. Las demás ($PWD, $backup…) se dejan intactas.
    /// </summary>
    public ObservableCollection<TemplateVariableViewModel> Variables { get; } = [];

    /// <summary>
    /// Comando con las variables reemplazadas. Una variable vacía se deja como
    /// <c>$nombre</c> para que se note qué falta.
    /// </summary>
    public string RenderedCommand => TemplateRenderer.Render(
        CommandTemplate,
        Variables.Where(v => !string.IsNullOrEmpty(v.Value)).ToDictionary(v => v.Name, v => v.Value));

    /// <summary>Carga una página (o nada) en el editor.</summary>
    public void Load(PageListItemViewModel? item)
    {
        // Cargar otra página siempre sale de la edición (la navegación está bloqueada
        // mientras se edita, así que aquí solo se llega tras Guardar/Cancelar o al eliminar).
        if (IsEditing)
        {
            ExitEditing();
        }

        foreach (var variable in Variables)
        {
            variable.PropertyChanged -= OnVariableChanged;
        }

        foreach (var previous in Fields)
        {
            previous.Conceal();
        }

        Page = item;
        Fields.Clear();
        Tags.Clear();
        Variables.Clear();
        CommandTemplate = string.Empty;
        HasTemplatePreview = false;

        if (item is not null)
        {
            foreach (var definition in _definitions)
            {
                var value = item.Model.Fields.FirstOrDefault(f => f.FieldDefinitionId == definition.Id);
                if (value is null || string.IsNullOrWhiteSpace(value.Value))
                {
                    continue;
                }

                var field = new FieldViewModel(definition, value, CopySensitiveField);
                if (field.Type == FieldType.Tags)
                {
                    foreach (var tag in field.Tags)
                    {
                        Tags.Add(tag);
                    }
                }
                else
                {
                    Fields.Add(field);
                }
            }

            var template = Fields.FirstOrDefault(f => f.IsCommandTemplate && !f.IsSensitive);
            CommandTemplate = template?.Model.Value ?? string.Empty;
            HasTemplatePreview = TemplateRenderer.UsesSupportedVariables(CommandTemplate);

            foreach (var name in TemplateRenderer.ExtractVariables(CommandTemplate)
                         .Where(n => TemplateRenderer.SupportedVariables.Contains(n, StringComparer.Ordinal)))
            {
                var current = _variableValues.GetValueOrDefault((item.Model.Id, name)) ?? InitialValue(item, name);
                var variable = new TemplateVariableViewModel(name, current);
                variable.PropertyChanged += OnVariableChanged;
                Variables.Add(variable);
            }
        }

        OnPropertyChanged(nameof(TypeLabel));
        OnPropertyChanged(nameof(UpdatedAtText));
        OnPropertyChanged(nameof(CharacterCountText));
        OnPropertyChanged(nameof(HasSensitiveFields));
        OnPropertyChanged(nameof(HasTags));
        OnPropertyChanged(nameof(RenderedCommand));
        OnPropertyChanged(nameof(PreviewCopyText));
    }

    /// <summary>
    /// Valor inicial de una variable: deducido del campo "Ejemplo" si así está configurado
    /// (repopack "$archivo" + repopack "D:\x\" → D:\x\); si no, su valor predeterminado.
    /// </summary>
    private string InitialValue(PageListItemViewModel item, string name)
    {
        if (Settings.FileVariableSource == FileVariableSource.Example)
        {
            var example = _definitions.FirstOrDefault(d => d.Key == "example");
            if (example is not null &&
                TemplateRenderer.TryExtractValue(CommandTemplate, item.Model.GetValue(example), name, out var fromExample))
            {
                return fromExample;
            }
        }

        return DefaultValue(name);
    }

    /// <summary>Valores predeterminados de la maqueta: rutas de demostración y la fecha de hoy.</summary>
    private static string DefaultValue(string name) => name switch
    {
        "archivo" or "ruta" => DemoFileValue,
        "fecha" => DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        _ => string.Empty
    };

    private void OnVariableChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (Page is not null && sender is TemplateVariableViewModel variable)
        {
            _variableValues[(Page.Model.Id, variable.Name)] = variable.Value;
        }

        OnPropertyChanged(nameof(RenderedCommand));
        OnPropertyChanged(nameof(PreviewCopyText));
    }

    [RelayCommand]
    private void PickFile(TemplateVariableViewModel? variable)
    {
        if (variable is null)
        {
            return;
        }

        var path = _fileDialogs.PickFile($"Elegir archivo para {variable.Token}");
        if (path is not null)
        {
            variable.Value = path;
        }
    }

    [RelayCommand]
    private void PickFolder(TemplateVariableViewModel? variable)
    {
        if (variable is null)
        {
            return;
        }

        var path = _fileDialogs.PickFolder($"Elegir carpeta para {variable.Token}");
        if (path is not null)
        {
            // Se mantiene la barra final, igual que en el ejemplo de la especificación.
            variable.Value = path.EndsWith('\\') ? path : path + "\\";
        }
    }

    // El parámetro no se usa para decidir: WPF puede consultar CanExecute antes de enlazarlo.
    private bool CanPasteVariable(TemplateVariableViewModel? variable) => Settings.UseClipboardForFileVariable;

    /// <summary>
    /// Usa el texto del portapapeles como valor de la variable (solo al pulsar el botón).
    /// Según la configuración: rechaza textos que parecen secretos y confirma antes de reemplazar.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPasteVariable))]
    private void PasteVariable(TemplateVariableViewModel? variable)
    {
        if (variable is null)
        {
            return;
        }

        var text = _clipboard.TryGetText()?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            Notice.Show("El portapapeles no contiene texto.", NoticeTone.Warning);
            return;
        }

        if (text.Contains('\n'))
        {
            Notice.Show("El texto tiene varias líneas: pega un solo valor.", NoticeTone.Warning);
            return;
        }

        if (Settings.RejectSensitivePaste && CopyPasteSettingsViewModel.LooksSensitive(text))
        {
            // No se muestra el texto: podría ser un secreto.
            Notice.Show("El texto parece un dato sensible y no se pegó.", NoticeTone.Warning);
            return;
        }

        if (variable.Value == text)
        {
            return;
        }

        if (Settings.ConfirmBeforeReplace && !string.IsNullOrWhiteSpace(variable.Value) &&
            !_confirmation.Confirm("Reemplazar valor", $"¿Reemplazar el valor actual de {variable.Token}?", "Reemplazar"))
        {
            return;
        }

        variable.Value = text;
    }

    [RelayCommand]
    private void ResetVariable(TemplateVariableViewModel? variable)
    {
        if (variable is not null)
        {
            variable.Value = DefaultValue(variable.Name);
        }
    }

    // ===================== Acciones de página (etapa 5) =====================

    /// <summary>
    /// Copia la página: el comando renderizado si hay plantilla; si no, el comando;
    /// si no hay comando, el contenido no sensible completo.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanUsePageActions))]
    private void Copy()
    {
        var text = BuildCopyText();
        if (string.IsNullOrEmpty(text))
        {
            Notice.Show("La página no tiene contenido para copiar.", NoticeTone.Warning);
            return;
        }

        if (!_clipboard.TrySetText(text))
        {
            Notice.Show("No se pudo copiar: el portapapeles está ocupado. Inténtalo de nuevo.", NoticeTone.Error);
            return;
        }

        Notice.Show(HasSensitiveFields
            ? "Copiado al portapapeles (sin los datos sensibles)"
            : "Copiado al portapapeles");
    }

    /// <summary>Texto exacto que produciría "Copiar" (lo muestra el panel ⚙).</summary>
    public string PreviewCopyText => HasPage ? BuildCopyText() : string.Empty;

    /// <summary>
    /// Arma el texto según el modo de copiado. Nunca incluye campos sensibles.
    /// <list type="bullet">
    /// <item>Original: la plantilla sin reemplazar (o el contenido completo si no hay comando).</item>
    /// <item>Renderizado: el comando con $archivo reemplazado.</item>
    /// <item>Bloque de código: lo anterior dentro de ``` ```.</item>
    /// <item>Campos seleccionados: los valores de los campos marcados en ⚙.</item>
    /// </list>
    /// </summary>
    private string BuildCopyText()
    {
        var command = Fields.FirstOrDefault(f => f.IsCommandTemplate && !f.IsSensitive);
        var fullText = string.Join(
            Environment.NewLine + Environment.NewLine,
            Fields.Where(f => !f.IsSensitive).Select(f => f.Model.Value));

        switch (Settings.CopyMode)
        {
            case CopyMode.Original:
                return command?.Model.Value ?? fullText;

            case CopyMode.CodeBlock:
                var code = HasTemplatePreview ? RenderedCommand : command?.Model.Value ?? fullText;
                return code.Length == 0 ? string.Empty : $"```{Environment.NewLine}{code}{Environment.NewLine}```";

            case CopyMode.SelectedFields:
                var selected = Settings.SelectedDefinitions.Select(d => d.Id).ToHashSet();
                return string.Join(
                    Environment.NewLine + Environment.NewLine,
                    Fields.Where(f => !f.IsSensitive && selected.Contains(f.Definition.Id))
                        .Select(f => f.IsCommandTemplate && HasTemplatePreview ? RenderedCommand : f.Model.Value));

            default: // Rendered
                if (HasTemplatePreview)
                {
                    return RenderedCommand;
                }

                return command?.Model.Value ?? fullText;
        }
    }

    [RelayCommand(CanExecute = nameof(CanUsePageActions))]
    private void Duplicate()
    {
        if (Page is { } current)
        {
            _duplicate(current);
            Notice.Show("Página duplicada (solo en memoria)");
        }
    }

    [RelayCommand(CanExecute = nameof(CanUsePageActions))]
    private void Delete()
    {
        if (Page is not { } current)
        {
            return;
        }

        if (_confirmation.Confirm("Eliminar página", $"¿Eliminar «{current.Title}»?\nEn la maqueta solo se quita de la memoria.", "Eliminar"))
        {
            _delete(current);
            Notice.Show("Página eliminada");
        }
    }

    [RelayCommand(CanExecute = nameof(CanUsePageActions))]
    private void ToggleFavorite()
    {
        if (Page is not null)
        {
            Page.IsFavorite = !Page.IsFavorite;
        }
    }

    // ===================== Datos sensibles (etapa 7) =====================

    /// <summary>
    /// Copia un campo sensible solo tras confirmar. Se marca para que el historial de
    /// Windows y la nube no lo guarden. El aviso nunca incluye el valor.
    /// </summary>
    private void CopySensitiveField(FieldViewModel field)
    {
        if (!field.HasValue)
        {
            return;
        }

        var confirmed = _confirmation.Confirm(
            "Copiar dato sensible",
            $"«{field.Label}» es un dato sensible.\n" +
            "Quedará en el portapapeles hasta que copies otra cosa. " +
            "Se pide a Windows que no lo guarde en su historial ni lo sincronice.",
            "Copiar el dato sensible");

        if (!confirmed)
        {
            return;
        }

        Notice.Show(_clipboard.TrySetSensitiveText(field.Model.Value)
            ? $"«{field.Label}» copiado. Recuerda reemplazarlo en el portapapeles al terminar."
            : "No se pudo copiar: el portapapeles está ocupado. Inténtalo de nuevo.",
            NoticeTone.Warning);
    }

    // ===================== Modo edición (etapa 8) =====================

    /// <summary>Borradores de los campos en edición (todos los definidos + temporales).</summary>
    public ObservableCollection<EditableFieldViewModel> EditFields { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUsePageActions), nameof(IsReading))]
    [NotifyCanExecuteChangedFor(nameof(EditCommand), nameof(SaveCommand), nameof(CancelEditCommand),
        nameof(CopyCommand), nameof(DuplicateCommand), nameof(DeleteCommand), nameof(ToggleFavoriteCommand),
        nameof(AddFieldCommand))]
    private bool isEditing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TitleError))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string editTitle = string.Empty;

    [ObservableProperty]
    private bool isDirty;

    public bool IsReading => HasPage && !IsEditing;

    public string? TitleError => IsEditing && string.IsNullOrWhiteSpace(EditTitle) ? "El título es obligatorio." : null;

    private bool CanEdit() => HasPage && !IsEditing;

    /// <summary>Entra en edición: borradores de título y de todos los campos definidos.</summary>
    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void Edit()
    {
        if (Page is not { } current)
        {
            return;
        }

        foreach (var field in Fields)
        {
            field.Conceal();
        }

        EditFields.Clear();
        foreach (var definition in _definitions)
        {
            var value = current.Model.Fields.FirstOrDefault(f => f.FieldDefinitionId == definition.Id)?.Value ?? string.Empty;
            EditFields.Add(new EditableFieldViewModel(definition, value, _ => RecomputeDirty()));
        }

        EditTitle = current.Title;
        IsEditing = true;
        IsDirty = false;
    }

    private bool CanAddField() => IsEditing;

    /// <summary>"+ Agregar campo": campo temporal en memoria (se crea su definición al guardar).</summary>
    [RelayCommand(CanExecute = nameof(CanAddField))]
    private void AddField()
    {
        var number = EditFields.Count(f => f.IsTemporary) + 1;
        EditFields.Add(new EditableFieldViewModel(
            number == 1 ? "Campo nuevo" : $"Campo nuevo {number}",
            _ => RecomputeDirty(),
            RemoveTemporaryField));
        RecomputeDirty();
    }

    private void RemoveTemporaryField(EditableFieldViewModel field)
    {
        EditFields.Remove(field);
        RecomputeDirty();
    }

    private bool CanSave() => IsEditing && !string.IsNullOrWhiteSpace(EditTitle);

    /// <summary>Ctrl+S: aplica los borradores al modelo en memoria.</summary>
    [RelayCommand(CanExecute = nameof(CanSave))]
    public void Save()
    {
        if (Page is not { } current || !CanSave())
        {
            return;
        }

        foreach (var draft in EditFields)
        {
            var definition = draft.Definition;
            if (definition is null)
            {
                // Campo temporal: se omite si quedó vacío; si no, se crea su definición en memoria.
                if (string.IsNullOrWhiteSpace(draft.Value))
                {
                    continue;
                }

                definition = new FieldDefinition
                {
                    Key = "custom-" + Guid.NewGuid().ToString("N")[..8],
                    Label = string.IsNullOrWhiteSpace(draft.Label) ? "Campo" : draft.Label.Trim(),
                    Type = draft.Type,
                    IsSensitive = draft.Type == FieldType.Secret,
                    DisplayOrder = _definitions.Count == 0 ? 0 : _definitions.Max(d => d.DisplayOrder) + 1
                };
                _definitions.Add(definition);
                Settings.AddFieldOption(definition);
            }

            var stored = current.Model.Fields.FirstOrDefault(f => f.FieldDefinitionId == definition.Id);
            if (stored is null)
            {
                current.Model.Fields.Add(new FieldValue { FieldDefinitionId = definition.Id, Value = draft.Value });
            }
            else
            {
                stored.Value = draft.Value;
            }
        }

        current.Model.IsSensitive = _definitions
            .Where(d => d.IsSensitive || d.Type == FieldType.Secret)
            .Any(d => !string.IsNullOrWhiteSpace(current.Model.GetValue(d)));
        current.Model.UpdatedAt = DateTime.Now;
        current.Title = EditTitle.Trim();
        current.Model.Title = current.Title;

        ExitEditing();
        current.NotifyContentChanged();
        Load(current);
        _saved(current);
        Notice.Show("Cambios guardados (solo en memoria)");
    }

    private bool CanCancelEdit() => IsEditing;

    /// <summary>Cancelar o Escape: si hay cambios, pregunta guardar / descartar / seguir.</summary>
    [RelayCommand(CanExecute = nameof(CanCancelEdit))]
    private void CancelEdit()
    {
        if (!IsDirty)
        {
            ExitEditing();
            return;
        }

        switch (_confirmation.AskUnsavedChanges(Page?.Title ?? "La página"))
        {
            case UnsavedChangesDecision.Save:
                Save();
                break;
            case UnsavedChangesDecision.Discard:
                ExitEditing();
                break;
            case UnsavedChangesDecision.ContinueEditing:
                break;
        }
    }

    private void ExitEditing()
    {
        IsEditing = false;
        IsDirty = false;
        EditFields.Clear();
        EditTitle = string.Empty;
    }

    partial void OnEditTitleChanged(string value) => RecomputeDirty();

    private void RecomputeDirty()
    {
        if (!IsEditing || Page is null)
        {
            return;
        }

        IsDirty = !string.Equals(EditTitle, Page.Title, StringComparison.Ordinal)
                  || EditFields.Any(f => f.IsDirty);
    }
}
