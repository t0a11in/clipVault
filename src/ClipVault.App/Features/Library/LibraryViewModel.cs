using System.Collections.ObjectModel;
using System.ComponentModel;
using ClipVault.App.Features.Library.Editor;
using ClipVault.App.Features.Library.Settings;
using ClipVault.App.Services;
using ClipVault.Core.Interfaces;
using ClipVault.Core.Models.Library;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClipVault.App.Features.Library;

/// <summary>
/// ViewModel de la Biblioteca.
/// Etapa 4: navegación (filtros/secciones) + lista de páginas con búsqueda,
/// contadores y creación de páginas.
/// Etapas 5 y 6: editor de la página seleccionada (<see cref="Editor"/>), con
/// duplicar/eliminar y plantilla $archivo. Todo en memoria (maqueta).
/// </summary>
public partial class LibraryViewModel : ObservableObject
{
    // Colores que se asignan en rotación a las secciones nuevas.
    private static readonly string[] SectionPalette =
        ["#4F46E5", "#2563EB", "#15803D", "#DB2777", "#B45309", "#0F766E", "#7E22CE", "#475569"];

    // Lista mutable: "+ Agregar campo" puede crear definiciones nuevas en memoria.
    private readonly List<FieldDefinition> _fieldDefinitions;
    private readonly List<PageListItemViewModel> _allPages;

    public LibraryViewModel(
        ILibraryDataSource dataSource,
        IClipboardService clipboard,
        IFileDialogService fileDialogs,
        IConfirmationService confirmation)
    {
        _fieldDefinitions = dataSource.GetFieldDefinitions().OrderBy(f => f.DisplayOrder).ToList();

        Notice = new NoticeViewModel();
        Settings = new CopyPasteSettingsViewModel(_fieldDefinitions);
        Editor = new PageEditorViewModel(
            _fieldDefinitions, clipboard, fileDialogs, confirmation, Notice, Settings,
            DuplicatePage, DeletePage, OnPageSaved);

        // Mientras se edita, la navegación se bloquea: así no se pierden cambios sin aviso.
        Editor.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PageEditorViewModel.IsEditing))
            {
                OnPropertyChanged(nameof(IsNavigationLocked));
                NewPageCommand.NotifyCanExecuteChanged();
                NewSectionCommand.NotifyCanExecuteChanged();
                RenameSelectedSectionCommand.NotifyCanExecuteChanged();
                EscapeCommand.NotifyCanExecuteChanged();
            }
        };

        Filters =
        [
            new FilterNavItemViewModel(LibraryFilterKind.All, "Todos los elementos", LibraryIcons.All),
            new FilterNavItemViewModel(LibraryFilterKind.Favorites, "Favoritos", LibraryIcons.Favorites)
        ];

        Sections = new ObservableCollection<SectionViewModel>(
            dataSource.GetSections()
                .OrderBy(section => section.Order)
                .Select(section => new SectionViewModel(section, DiscardNewSection)));

        _allPages = dataSource.GetPages().Select(CreatePageItem).ToList();

        SelectedSection = Sections.FirstOrDefault();
        RefreshPages();
    }

    /// <summary>Editor de la página seleccionada (etapas 5 y 6).</summary>
    public PageEditorViewModel Editor { get; }

    /// <summary>Aviso breve compartido ("Copiado al portapapeles").</summary>
    public NoticeViewModel Notice { get; }

    /// <summary>Configuración de copiado y pegado (panel ⚙, etapa 9).</summary>
    public CopyPasteSettingsViewModel Settings { get; }

    [ObservableProperty]
    private bool isSettingsOpen;

    [RelayCommand]
    private void OpenSettings() => IsSettingsOpen = true;

    [RelayCommand]
    private void CloseSettings() => IsSettingsOpen = false;

    private bool CanHandleEscape() => IsSettingsOpen || Editor.IsEditing;

    /// <summary>
    /// Escape según el contexto: primero cierra el panel ⚙; si se está editando,
    /// cancela la edición (y pregunta si hay cambios).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanHandleEscape))]
    private void Escape()
    {
        if (IsSettingsOpen)
        {
            IsSettingsOpen = false;
        }
        else if (Editor.CancelEditCommand.CanExecute(null))
        {
            Editor.CancelEditCommand.Execute(null);
        }
    }

    partial void OnIsSettingsOpenChanged(bool value) => EscapeCommand.NotifyCanExecuteChanged();

    /// <summary>
    /// Verdadero mientras se edita una página: navegación y lista quedan bloqueadas
    /// hasta Guardar o Cancelar (que ofrece guardar, descartar o seguir editando).
    /// </summary>
    public bool IsNavigationLocked => Editor.IsEditing;

    // ===================== Navegación =====================

    public ObservableCollection<FilterNavItemViewModel> Filters { get; }

    public ObservableCollection<SectionViewModel> Sections { get; }

    /// <summary>Filtro activo. Es excluyente con <see cref="SelectedSection"/>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Breadcrumb))]
    private FilterNavItemViewModel? selectedFilter;

    /// <summary>Sección activa. Es excluyente con <see cref="SelectedFilter"/>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Breadcrumb))]
    [NotifyCanExecuteChangedFor(nameof(RenameSelectedSectionCommand))]
    [NotifyCanExecuteChangedFor(nameof(NewPageCommand))]
    private SectionViewModel? selectedSection;

    private NavItemViewModel? CurrentNavItem => (NavItemViewModel?)SelectedSection ?? SelectedFilter;

    /// <summary>Ruta de la barra superior: "Biblioteca › Comandos › IA Clonar".</summary>
    public string Breadcrumb
    {
        get
        {
            var parts = new List<string> { "Biblioteca" };
            if (CurrentNavItem is { } nav)
            {
                parts.Add(nav.Label);
            }

            if (SelectedPage is { } page)
            {
                parts.Add(page.Title);
            }

            return string.Join("  ›  ", parts);
        }
    }

    // Una sola selección en todo el panel: elegir un filtro limpia la sección y viceversa.
    partial void OnSelectedFilterChanged(FilterNavItemViewModel? value)
    {
        if (value is not null)
        {
            SelectedSection = null;
        }

        RefreshPages();
    }

    partial void OnSelectedSectionChanged(SectionViewModel? oldValue, SectionViewModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= OnNavOrPagePropertyChanged;
        }

        if (newValue is not null)
        {
            newValue.PropertyChanged += OnNavOrPagePropertyChanged;
            SelectedFilter = null;
        }

        RefreshPages();
    }

    private bool CanCreateSection() => !IsNavigationLocked;

    /// <summary>Ctrl+Shift+N: crea una sección en memoria y la deja en modo renombrar.</summary>
    [RelayCommand(CanExecute = nameof(CanCreateSection))]
    private void NewSection()
    {
        var model = new Section
        {
            Name = NextUniqueName("Nueva sección", Sections.Select(s => s.Label)),
            ColorHex = SectionPalette[Sections.Count % SectionPalette.Length],
            IconKey = "folder",
            Order = Sections.Count
        };

        var section = new SectionViewModel(model, DiscardNewSection, isNew: true);
        Sections.Add(section);
        SelectedSection = section;
        section.BeginRename();
    }

    private bool CanRenameSelectedSection() => SelectedSection is not null && !IsNavigationLocked;

    /// <summary>F2: renombrar la sección activa.</summary>
    [RelayCommand(CanExecute = nameof(CanRenameSelectedSection))]
    private void RenameSelectedSection() => SelectedSection?.BeginRename();

    private void DiscardNewSection(SectionViewModel section)
    {
        var index = Sections.IndexOf(section);
        Sections.Remove(section);

        if (ReferenceEquals(SelectedSection, section) || SelectedSection is null)
        {
            SelectedSection = Sections.Count == 0
                ? null
                : Sections[Math.Clamp(index - 1, 0, Sections.Count - 1)];
        }

        if (SelectedSection is null && SelectedFilter is null)
        {
            SelectedFilter = Filters[0];
        }
    }

    // ===================== Páginas =====================

    /// <summary>Páginas visibles según la sección o filtro activo y la búsqueda.</summary>
    public ObservableCollection<PageListItemViewModel> VisiblePages { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Breadcrumb))]
    [NotifyPropertyChangedFor(nameof(HasSelectedPage))]
    private PageListItemViewModel? selectedPage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSearch))]
    private string searchText = string.Empty;

    public bool HasSelectedPage => SelectedPage is not null;

    public bool HasSearch => !string.IsNullOrWhiteSpace(SearchText);

    /// <summary>Hay páginas en el contexto actual, pero la búsqueda no encontró ninguna.</summary>
    public bool IsNoSearchResults => HasSearch && VisiblePages.Count == 0;

    /// <summary>El contexto actual (sección o filtro) no tiene páginas.</summary>
    public bool IsContextEmpty => !HasSearch && VisiblePages.Count == 0;

    public string EmptyTitle => SelectedFilter?.Kind switch
    {
        LibraryFilterKind.Favorites => "Aún no tienes favoritos",
        LibraryFilterKind.All => "La biblioteca está vacía",
        _ => "Esta sección aún no tiene páginas"
    };

    public string EmptyDescription => SelectedFilter?.Kind == LibraryFilterKind.Favorites
        ? "Marca una página con la estrella para verla aquí."
        : "Guarda tu primer mensaje, comando, prompt o fragmento.";

    partial void OnSearchTextChanged(string value) => RefreshPages();

    partial void OnSelectedPageChanged(PageListItemViewModel? oldValue, PageListItemViewModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= OnNavOrPagePropertyChanged;
        }

        if (newValue is not null)
        {
            newValue.PropertyChanged += OnNavOrPagePropertyChanged;
        }

        // Si solo se restauró la misma página (al refrescar la lista), el editor no se recarga.
        if (!ReferenceEquals(Editor.Page, newValue))
        {
            Editor.Load(newValue);
        }
    }

    // Renombrar la sección o la página activa actualiza la ruta superior.
    private void OnNavOrPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(NavItemViewModel.Label) or nameof(PageListItemViewModel.Title))
        {
            OnPropertyChanged(nameof(Breadcrumb));
        }

        if (sender is PageListItemViewModel && e.PropertyName == nameof(PageListItemViewModel.IsFavorite))
        {
            UpdateCounts();
        }
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = string.Empty;

    private bool CanCreatePage() => SelectedSection is not null && !IsNavigationLocked;

    /// <summary>Ctrl+N: crea una página en la sección activa y la deja en modo renombrar.</summary>
    [RelayCommand(CanExecute = nameof(CanCreatePage))]
    private void NewPage()
    {
        if (SelectedSection is not { } section)
        {
            return;
        }

        var sectionPages = _allPages.Where(p => p.Model.SectionId == section.Model.Id).Select(p => p.Title);
        var model = new LibraryPage
        {
            SectionId = section.Model.Id,
            Title = NextUniqueName("Nueva página", sectionPages),
            Fields = _fieldDefinitions
                .Where(f => !f.IsSensitive)
                .Select(f => new FieldValue { FieldDefinitionId = f.Id })
                .ToList()
        };

        var page = CreatePageItem(model, isNew: true);
        _allPages.Add(page);

        // La búsqueda podría ocultar la página nueva: se limpia antes de mostrarla.
        if (HasSearch)
        {
            SearchText = string.Empty;
        }

        RefreshPages();
        SelectedPage = page;
        page.Rename.Begin();
    }

    private PageListItemViewModel CreatePageItem(LibraryPage model) => CreatePageItem(model, isNew: false);

    private PageListItemViewModel CreatePageItem(LibraryPage model, bool isNew) =>
        new(model, _fieldDefinitions, DiscardNewPage, isNew);

    /// <summary>Duplica la página en la misma sección (en memoria) y la selecciona.</summary>
    private void DuplicatePage(PageListItemViewModel source)
    {
        var sectionTitles = _allPages
            .Where(p => p.Model.SectionId == source.Model.SectionId)
            .Select(p => p.Title);

        var copy = new LibraryPage
        {
            SectionId = source.Model.SectionId,
            Title = NextUniqueName($"{source.Title} (copia)", sectionTitles),
            IsSensitive = source.Model.IsSensitive,
            IsFavorite = false,
            Fields = source.Model.Fields
                .Select(f => new FieldValue { FieldDefinitionId = f.FieldDefinitionId, Value = f.Value })
                .ToList()
        };

        var item = CreatePageItem(copy);
        _allPages.Add(item);
        if (HasSearch)
        {
            SearchText = string.Empty;
        }

        RefreshPages();
        SelectedPage = item;
    }

    /// <summary>Tras guardar en el editor: reordena la lista y actualiza contadores.</summary>
    private void OnPageSaved(PageListItemViewModel page)
    {
        RefreshPages();
        OnPropertyChanged(nameof(Breadcrumb));
    }

    /// <summary>Quita la página de la memoria (la confirmación la pide el editor).</summary>
    private void DeletePage(PageListItemViewModel page)
    {
        _allPages.Remove(page);
        if (ReferenceEquals(SelectedPage, page))
        {
            SelectedPage = null;
        }

        RefreshPages();
    }

    private void DiscardNewPage(PageListItemViewModel page)
    {
        _allPages.Remove(page);
        if (ReferenceEquals(SelectedPage, page))
        {
            SelectedPage = null;
        }

        RefreshPages();
    }

    /// <summary>
    /// Recalcula la lista visible: contexto (sección, todos o favoritos) + búsqueda,
    /// ordenada por última modificación. Conserva la página seleccionada si sigue visible.
    /// </summary>
    private void RefreshPages()
    {
        // Durante la construcción las colecciones aún pueden no existir.
        if (_allPages is null)
        {
            return;
        }

        IEnumerable<PageListItemViewModel> query = _allPages;

        if (SelectedSection is { } section)
        {
            query = query.Where(p => p.Model.SectionId == section.Model.Id);
        }
        else if (SelectedFilter?.Kind == LibraryFilterKind.Favorites)
        {
            query = query.Where(p => p.IsFavorite);
        }

        var showSection = SelectedSection is null;
        var sectionNames = Sections.ToDictionary(s => s.Model.Id, s => s.Label);
        var visible = query
            .Where(p => p.Matches(SearchText))
            .OrderByDescending(p => p.Model.UpdatedAt)
            .ToList();

        // Al vaciar la colección, el ListBox enlazado pone la selección en null;
        // por eso se guarda antes y se restaura si la página sigue visible.
        var previousSelection = SelectedPage;

        VisiblePages.Clear();
        foreach (var page in visible)
        {
            page.ShowSectionName = showSection;
            page.SectionName = sectionNames.GetValueOrDefault(page.Model.SectionId, string.Empty);
            VisiblePages.Add(page);
        }

        SelectedPage = previousSelection is not null && visible.Contains(previousSelection)
            ? previousSelection
            : null;

        UpdateCounts();
        OnPropertyChanged(nameof(IsNoSearchResults));
        OnPropertyChanged(nameof(IsContextEmpty));
        OnPropertyChanged(nameof(EmptyTitle));
        OnPropertyChanged(nameof(EmptyDescription));
    }

    private void UpdateCounts()
    {
        foreach (var section in Sections)
        {
            section.Count = _allPages.Count(p => p.Model.SectionId == section.Model.Id);
        }

        Filters[0].Count = _allPages.Count;
        Filters[1].Count = _allPages.Count(p => p.IsFavorite);
    }

    private static string NextUniqueName(string baseName, IEnumerable<string> existing)
    {
        var names = existing.ToHashSet(StringComparer.CurrentCultureIgnoreCase);
        if (!names.Contains(baseName))
        {
            return baseName;
        }

        var number = 2;
        while (names.Contains($"{baseName} {number}"))
        {
            number++;
        }

        return $"{baseName} {number}";
    }
}
