using ClipVault.Core.Models.Library;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClipVault.App.Features.Library.Editor;

/// <summary>
/// Borrador de un campo en modo edición (etapa 8). Los cambios no tocan el modelo
/// hasta "Guardar". Un campo temporal (agregado con "+ Agregar campo") permite
/// elegir etiqueta y tipo; al guardar se crea su definición en memoria.
/// </summary>
public sealed partial class EditableFieldViewModel : ObservableObject
{
    /// <summary>Tipos que se pueden elegir al agregar un campo en la maqueta.</summary>
    public static IReadOnlyList<FieldTypeOption> TypeOptions { get; } =
    [
        new(FieldType.Text, "Texto"),
        new(FieldType.MultilineText, "Texto multilínea"),
        new(FieldType.Code, "Código o comando"),
        new(FieldType.FilePath, "Ruta de archivo"),
        new(FieldType.Secret, "Dato sensible")
    ];

    private readonly Action<EditableFieldViewModel> _onChanged;
    private readonly Action<EditableFieldViewModel>? _onRemove;

    /// <summary>Campo existente (definición conocida).</summary>
    public EditableFieldViewModel(FieldDefinition definition, string value, Action<EditableFieldViewModel> onChanged)
    {
        Definition = definition;
        label = definition.Label;
        type = definition.Type;
        this.value = value;
        OriginalValue = value;
        _onChanged = onChanged;
    }

    /// <summary>Campo temporal nuevo.</summary>
    public EditableFieldViewModel(string label, Action<EditableFieldViewModel> onChanged, Action<EditableFieldViewModel> onRemove)
    {
        this.label = label;
        type = FieldType.Text;
        value = string.Empty;
        OriginalValue = string.Empty;
        IsTemporary = true;
        _onChanged = onChanged;
        _onRemove = onRemove;
        selectedTypeOption = TypeOptions[0];
    }

    public FieldDefinition? Definition { get; }

    public bool IsTemporary { get; }

    public string OriginalValue { get; }

    [ObservableProperty]
    private string label;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSensitive), nameof(IsCode), nameof(IsMultiline))]
    private FieldType type;

    [ObservableProperty]
    private string value;

    /// <summary>Solo campos temporales: tipo elegido en el selector.</summary>
    [ObservableProperty]
    private FieldTypeOption? selectedTypeOption;

    public bool IsSensitive => Type == FieldType.Secret || Definition?.IsSensitive == true;

    public bool IsCode => Type == FieldType.Code;

    public bool IsMultiline => Type is FieldType.MultilineText or FieldType.Code;

    public bool IsDirty => IsTemporary || !string.Equals(Value, OriginalValue, StringComparison.Ordinal);

    partial void OnValueChanged(string value) => _onChanged(this);

    partial void OnLabelChanged(string value) => _onChanged(this);

    partial void OnSelectedTypeOptionChanged(FieldTypeOption? value)
    {
        if (value is not null)
        {
            Type = value.Type;
        }

        _onChanged(this);
    }

    [RelayCommand]
    private void Remove() => _onRemove?.Invoke(this);
}

/// <summary>Opción del selector de tipo de campo.</summary>
public sealed record FieldTypeOption(FieldType Type, string Label);
