using ClipVault.Core.Models.Library;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClipVault.App.Features.Library.Editor;

/// <summary>
/// Campo de una página (definición + valor) listo para dibujarse según su tipo.
/// Etapa 7 — datos sensibles:
/// <list type="bullet">
/// <item>Se muestran como <see cref="Mask"/> salvo que el usuario pulse "Mostrar temporalmente";
/// se vuelven a ocultar solos tras <see cref="RevealDuration"/>.</item>
/// <item>Copiarlos exige confirmación (la pide el editor mediante <c>copySensitive</c>).</item>
/// <item>El valor nunca se incluye en avisos, ToolTips ni registros.</item>
/// </list>
/// </summary>
public sealed partial class FieldViewModel : ObservableObject
{
    public const string Mask = "••••••••";
    public static readonly TimeSpan RevealDuration = TimeSpan.FromSeconds(10);

    private readonly Action<FieldViewModel>? _copySensitive;
    private CancellationTokenSource? _revealCancellation;

    public FieldViewModel(FieldDefinition definition, FieldValue value, Action<FieldViewModel>? copySensitive = null)
    {
        Definition = definition;
        Model = value;
        _copySensitive = copySensitive;
    }

    public FieldDefinition Definition { get; }

    public FieldValue Model { get; }

    public string Label => Definition.Label;

    public FieldType Type => Definition.Type;

    public bool IsSensitive => Definition.IsSensitive || Definition.Type == FieldType.Secret;

    public bool IsCommandTemplate => Definition.IsCommandTemplate;

    public bool HasValue => !string.IsNullOrWhiteSpace(Model.Value);

    /// <summary>Solo para campos sensibles: el valor está visible temporalmente.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayValue))]
    [NotifyPropertyChangedFor(nameof(RevealButtonText))]
    private bool isRevealed;

    /// <summary>Valor para mostrar: oculto si es sensible y no se pidió mostrarlo.</summary>
    public string DisplayValue => IsSensitive && !IsRevealed ? Mask : Model.Value;

    public string RevealButtonText => IsRevealed ? "Ocultar" : "Mostrar temporalmente";

    /// <summary>Etiquetas separadas por coma (solo para el tipo Tags).</summary>
    public IReadOnlyList<string> Tags => Type == FieldType.Tags
        ? Model.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        : [];

    /// <summary>Caracteres contabilizables (los sensibles no se cuentan ni se revelan).</summary>
    public int CharacterCount => IsSensitive ? 0 : Model.Value.Length;

    [RelayCommand]
    private void ToggleReveal()
    {
        _revealCancellation?.Cancel();

        if (IsRevealed)
        {
            IsRevealed = false;
            return;
        }

        IsRevealed = true;
        var cancellation = new CancellationTokenSource();
        _revealCancellation = cancellation;
        _ = HideAfterDelayAsync(cancellation.Token);
    }

    /// <summary>Oculta de inmediato (al cambiar de página o salir del editor).</summary>
    public void Conceal()
    {
        _revealCancellation?.Cancel();
        IsRevealed = false;
    }

    private async Task HideAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(RevealDuration, token);
            IsRevealed = false;
        }
        catch (TaskCanceledException)
        {
            // Se ocultó antes a mano o se volvió a pedir.
        }
    }

    /// <summary>Copia el dato sensible (el editor pide confirmación antes).</summary>
    [RelayCommand]
    private void CopySensitive() => _copySensitive?.Invoke(this);
}
