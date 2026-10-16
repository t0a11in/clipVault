using System.Globalization;
using ClipVault.Core.Models.Library;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClipVault.App.Features.Library;

/// <summary>
/// Página tal como se muestra en la lista: título, vista previa, fecha e indicadores.
/// La vista previa y la búsqueda nunca usan campos sensibles.
/// </summary>
public sealed partial class PageListItemViewModel : ObservableObject
{
    private static readonly CultureInfo DateCulture = CultureInfo.GetCultureInfo("es-CL");
    private readonly IReadOnlyList<FieldDefinition> _fields;

    public PageListItemViewModel(
        LibraryPage model,
        IReadOnlyList<FieldDefinition> fields,
        Action<PageListItemViewModel> onDiscardNew,
        bool isNew = false)
    {
        Model = model;
        _fields = fields;
        IsNew = isNew;
        title = model.Title;
        isFavorite = model.IsFavorite;

        Rename = new InlineRenameViewModel(
            currentName: () => Title,
            apply: name =>
            {
                Title = name;
                Model.Title = name;
                Model.UpdatedAt = DateTime.Now;
                IsNew = false;
                OnPropertyChanged(nameof(Meta));
            },
            onCancelled: () =>
            {
                if (IsNew)
                {
                    onDiscardNew(this);
                }
            });
    }

    public LibraryPage Model { get; }

    public bool IsNew { get; private set; }

    public InlineRenameViewModel Rename { get; }

    [ObservableProperty]
    private string title;

    [ObservableProperty]
    private bool isFavorite;

    /// <summary>Nombre de la sección; se muestra cuando la lista viene de un filtro global.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Meta))]
    private string sectionName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Meta))]
    private bool showSectionName;

    public bool IsSensitive => Model.IsSensitive;

    public bool HasCommand => _fields.Any(f => f.IsCommandTemplate && !string.IsNullOrWhiteSpace(Model.GetValue(f)));

    /// <summary>Primera línea del comando o, si no hay, de la descripción.</summary>
    public string Preview
    {
        get
        {
            var source = _fields
                .Where(f => !f.IsSensitive)
                .OrderByDescending(f => f.IsCommandTemplate)
                .ThenBy(f => f.DisplayOrder)
                .Select(Model.GetValue)
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;

            var firstLine = source.Split('\n', 2)[0].Trim();
            return firstLine.Length == 0 ? "Sin contenido" : firstLine;
        }
    }

    public string Meta
    {
        get
        {
            var date = Model.UpdatedAt.ToString("dd-MM-yyyy", DateCulture);
            return ShowSectionName && SectionName.Length > 0 ? $"{SectionName} · {date}" : date;
        }
    }

    public string ToolTip => IsSensitive ? $"{Title} · contiene datos sensibles" : Title;

    /// <summary>Avisa a la lista que el contenido cambió (tras guardar en el editor).</summary>
    public void NotifyContentChanged()
    {
        OnPropertyChanged(nameof(Preview));
        OnPropertyChanged(nameof(HasCommand));
        OnPropertyChanged(nameof(IsSensitive));
        OnPropertyChanged(nameof(Meta));
        OnPropertyChanged(nameof(ToolTip));
    }

    partial void OnTitleChanged(string value) => OnPropertyChanged(nameof(ToolTip));

    partial void OnIsFavoriteChanged(bool value) => Model.IsFavorite = value;

    /// <summary>
    /// Coincidencia de búsqueda en título y contenido no sensible,
    /// sin distinguir mayúsculas ni tildes.
    /// </summary>
    public bool Matches(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var compare = DateCulture.CompareInfo;
        const CompareOptions options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
        var term = query.Trim();

        if (compare.IndexOf(Title, term, options) >= 0)
        {
            return true;
        }

        return _fields
            .Where(f => !f.IsSensitive)
            .Any(f => compare.IndexOf(Model.GetValue(f), term, options) >= 0);
    }
}
