using System.Collections.ObjectModel;
using System.ComponentModel;
using ClipVault.App.Services;
using ClipVault.Core.Interfaces;
using ClipVault.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClipVault.App.ViewModels;

public partial class MainViewModel(
    IClipRepository repository,
    ISaveClipDialogService saveClipDialogService,
    ICategoryDialogService categoryDialogService,
    IApplicationWindowService applicationWindowService) : ObservableObject
{
    private readonly SemaphoreSlim _captureGate = new(1, 1);
    private bool _updatingSelectAllState;

    // Los clips recién copiados son temporales y la captura siempre les asigna Category = null.
    public ObservableCollection<ClipListItemViewModel> RecentClips { get; } = [];

    // Los clips guardados por el usuario son los que tienen IsPermanent = true.
    public ObservableCollection<ClipListItemViewModel> SavedClips { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SavePermanentCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteSelectedCommand))]
    private ClipListItemViewModel? selectedClip;

    // 0 = Recientes, 1 = Guardados. Las acciones masivas actúan solo sobre esta lista.
    [ObservableProperty]
    private int selectedTabIndex;

    [ObservableProperty]
    private string filterText = string.Empty;

    [ObservableProperty]
    private bool isFilterOpen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CaptureLockButtonText))]
    [NotifyPropertyChangedFor(nameof(CaptureLockToolTip))]
    private bool isCaptureBlocked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExtraActionsToggleText))]
    [NotifyPropertyChangedFor(nameof(ExtraActionsToggleToolTip))]
    private bool isExtraActionsExpanded;

    [ObservableProperty]
    private string status = "Listo.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPreviewOpen))]
    [NotifyPropertyChangedFor(nameof(IsTextPreview))]
    [NotifyPropertyChangedFor(nameof(IsImagePreview))]
    [NotifyPropertyChangedFor(nameof(PreviewImageData))]
    [NotifyCanExecuteChangedFor(nameof(SavePreviewTextCommand))]
    private ClipListItemViewModel? previewedClip;

    [ObservableProperty]
    private string editablePreviewText = string.Empty;

    [ObservableProperty]
    private bool? areRecentClipsMarked;

    [ObservableProperty]
    private bool? areSavedClipsMarked;

    public int MarkedForDeletionCount =>
        GetAllClipRows().Count(clip => clip.IsMarkedForDeletion);

    public bool HasMarkedForDeletion => MarkedForDeletionCount > 0;

    public bool HasRecentClips => RecentClips.Count > 0;

    public bool HasSavedClips => SavedClips.Count > 0;

    public bool HasClips => HasRecentClips || HasSavedClips;

    public bool HasActiveClips => GetActiveClipRows().Any();

    public int ActiveMarkedForDeletionCount =>
        GetActiveClipRows().Count(clip => clip.IsMarkedForDeletion);

    public bool HasActiveMarkedForDeletion => ActiveMarkedForDeletionCount > 0;

    // Sin checks se eliminan todos los clips de la pestaña abierta. Con checks,
    // el icono conserva la acción pero añade la cantidad que se eliminará.
    public string BulkDeleteButtonText => HasActiveMarkedForDeletion
        ? $"🗑 Eliminar {ActiveMarkedForDeletionCount}"
        : "🗑 Eliminar todos";

    public string BulkDeleteToolTip => HasActiveMarkedForDeletion
        ? $"Eliminar {ActiveMarkedForDeletionCount} clip(s) seleccionado(s) de esta pestaña"
        : "Eliminar todos los clips de esta pestaña";

    public string CaptureLockButtonText => IsCaptureBlocked
        ? "🔓 Desbloquear copiado"
        : "🔒 Bloquear copiado";

    public string CaptureLockToolTip => IsCaptureBlocked
        ? "Desbloquear captura de portapapeles"
        : "Bloquear captura de portapapeles";

    public string ExtraActionsToggleText => IsExtraActionsExpanded
        ? "− Ocultar"
        : "＋ Mostrar";

    public string ExtraActionsToggleToolTip => IsExtraActionsExpanded
        ? "Ocultar acciones secundarias"
        : "Mostrar Eliminar todos y Bloquear copiado";

    public bool IsPreviewOpen => PreviewedClip is not null;

    public bool IsTextPreview => PreviewedClip?.Type == ClipType.Text;

    public bool IsImagePreview => PreviewedClip?.Type == ClipType.Image;

    public byte[]? PreviewImageData => PreviewedClip?.ImageData;

    public async Task InitializeAsync()
    {
        await LoadAsync();
    }

    public async Task CaptureAsync(ClipboardContent content)
    {
        await _captureGate.WaitAsync();

        try
        {
            var clip = new ClipItem
            {
                Type = content.Type,
                Content = content.Text,
                ImageData = content.ImageData,
                CreatedAt = DateTime.UtcNow
            };

            await repository.AddTemporaryAsync(clip);

            if (MatchesCurrentFilter(clip))
            {
                AddClipRow(clip);
                UpdateSelectionState();
            }

            Status = content.Type == ClipType.Text
                ? "Texto capturado."
                : "Imagen PNG capturada.";
        }
        finally
        {
            _captureGate.Release();
        }
    }

    public void OpenPreview(ClipListItemViewModel clip)
    {
        PreviewedClip = clip;
        EditablePreviewText = clip.Type == ClipType.Text
            ? clip.Clip.Content ?? string.Empty
            : string.Empty;
    }

    [RelayCommand]
    private void ToggleFilter()
    {
        IsFilterOpen = !IsFilterOpen;

        // Al ocultar el campo se elimina el filtro para no dejar resultados
        // invisiblemente restringidos.
        if (!IsFilterOpen)
        {
            FilterText = string.Empty;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private void ToggleCaptureLock()
    {
        IsCaptureBlocked = !IsCaptureBlocked;
        Status = IsCaptureBlocked
            ? "Captura del portapapeles bloqueada."
            : "Captura del portapapeles activada.";
    }

    [RelayCommand]
    private void ToggleExtraActions()
    {
        IsExtraActionsExpanded = !IsExtraActionsExpanded;
    }

    [RelayCommand]
    private async Task OpenCategoriesAsync()
    {
        await categoryDialogService.ShowAsync();
        // Una edición o eliminación puede cambiar la categoría visible de clips ya
        // cargados; al cerrar el modal se vuelve a consultar SQLite.
        await LoadAsync();
    }

    [RelayCommand(CanExecute = nameof(CanModifySelected))]
    private async Task SavePermanentAsync()
    {
        if (SelectedClip is null)
        {
            return;
        }

        var request = await saveClipDialogService.ShowAsync(SelectedClip.Clip);

        if (request is null)
        {
            return;
        }

        var selectedId = SelectedClip.Id;

        await repository.SavePermanentlyAsync(
            selectedId,
            request.Title,
            request.Category);

        Status = "Clip guardado permanentemente.";
        await LoadAsync(selectedId);
    }

    [RelayCommand(CanExecute = nameof(CanModifySelected))]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedClip is null)
        {
            return;
        }

        await repository.DeleteAsync(SelectedClip.Id);

        SelectedClip = null;
        IsExtraActionsExpanded = false;
        Status = "Clip eliminado.";
        await LoadAsync();
    }

    [RelayCommand(CanExecute = nameof(HasClipsToDelete))]
    private async Task DeleteAllOrMarkedAsync()
    {
        // Se toma una instantánea de los checks al pulsar el botón. La decisión no usa
        // el texto del botón ni propiedades visuales que pudieran actualizarse después.
        var activeClips = GetActiveClipRows().ToList();
        var markedClips = activeClips
            .Where(clip => clip.IsMarkedForDeletion)
            .ToList();

        // Con al menos un check, esta colección es exclusivamente la marcada de la
        // pestaña abierta. Sin checks, solo se toma la pestaña abierta, como WhatsApp.
        var deletesMarkedClips = markedClips.Count > 0;
        var clipsToDelete = deletesMarkedClips
            ? markedClips
            : activeClips;

        foreach (var clip in clipsToDelete)
        {
            await repository.DeleteAsync(clip.Id);
        }

        SelectedClip = null;
        IsExtraActionsExpanded = false;
        Status = deletesMarkedClips
            ? $"{clipsToDelete.Count} clip(s) seleccionado(s) eliminado(s)."
            : $"{clipsToDelete.Count} clip(s) eliminado(s).";
        await LoadAsync();
    }

    [RelayCommand]
    private void ClosePreview()
    {
        PreviewedClip = null;
    }

    [RelayCommand(CanExecute = nameof(CanSavePreviewText))]
    private async Task SavePreviewTextAsync()
    {
        if (PreviewedClip is null || PreviewedClip.Type != ClipType.Text)
        {
            return;
        }

        await repository.UpdateTextAsync(PreviewedClip.Id, EditablePreviewText);
        PreviewedClip.UpdateText(EditablePreviewText);
        Status = "Texto actualizado.";
        ClosePreview();
    }

    [RelayCommand]
    private async Task SaveCurrentAsync()
    {
        var latest = await repository.GetLatestAsync();

        if (latest is null)
        {
            Status = "No hay clips para guardar.";
            return;
        }

        SelectedClip = GetAllClipRows()
            .FirstOrDefault(clip => clip.Id == latest.Id)
            ?? new ClipListItemViewModel(latest);

        await SavePermanentAsync();
    }

    [RelayCommand]
    private void Open()
    {
        applicationWindowService.ShowMainWindow();
    }

    [RelayCommand]
    private void Exit()
    {
        applicationWindowService.ExitApplication();
    }

    private bool CanModifySelected() =>
        SelectedClip is not null && GetActiveClipRows().Contains(SelectedClip);

    private bool HasClipsToDelete() => HasActiveClips;

    private bool CanSavePreviewText() => IsTextPreview;

    partial void OnFilterTextChanged(string value)
    {
        // El binding actualiza esta propiedad con cada pulsación de tecla.
        _ = LoadAsync();
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        // Una fila seleccionada pertenece a una sola pestaña. Al cambiarla se evita
        // aplicar "Eliminar" sobre una fila que ya no está a la vista.
        SelectedClip = null;
        UpdateSelectionState();
    }

    partial void OnAreRecentClipsMarkedChanged(bool? value)
    {
        MarkAll(RecentClips, value);
    }

    partial void OnAreSavedClipsMarkedChanged(bool? value)
    {
        MarkAll(SavedClips, value);
    }

    private async Task LoadAsync(long? selectedId = null)
    {
        var targetId = selectedId ?? SelectedClip?.Id;
        var clips = await repository.GetClipsAsync(FilterText);

        ClearClipRows();

        foreach (var clip in clips)
        {
            AddClipRow(clip);
        }

        SelectedClip = targetId is null
            ? null
            : GetAllClipRows().FirstOrDefault(clip => clip.Id == targetId);

        UpdateSelectionState();
        Status = $"{RecentClips.Count} reciente(s), {SavedClips.Count} guardado(s).";
    }

    private void AddClipRow(ClipItem clip)
    {
        var row = new ClipListItemViewModel(clip);
        row.PropertyChanged += OnClipRowPropertyChanged;

        if (clip.IsPermanent)
        {
            SavedClips.Add(row);
        }
        else
        {
            RecentClips.Add(row);
        }
    }

    private void ClearClipRows()
    {
        foreach (var row in GetAllClipRows().ToList())
        {
            row.PropertyChanged -= OnClipRowPropertyChanged;
        }

        RecentClips.Clear();
        SavedClips.Clear();
    }

    private void OnClipRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ClipListItemViewModel.IsMarkedForDeletion))
        {
            UpdateSelectionState();
        }
    }

    private void MarkAll(
        IEnumerable<ClipListItemViewModel> clips,
        bool? shouldMark)
    {
        if (_updatingSelectAllState || shouldMark is null)
        {
            return;
        }

        foreach (var clip in clips)
        {
            clip.IsMarkedForDeletion = shouldMark.Value;
        }

        UpdateSelectionState();
    }

    private void UpdateSelectionState()
    {
        _updatingSelectAllState = true;
        AreRecentClipsMarked = GetSelectAllState(RecentClips);
        AreSavedClipsMarked = GetSelectAllState(SavedClips);
        _updatingSelectAllState = false;

        OnPropertyChanged(nameof(MarkedForDeletionCount));
        OnPropertyChanged(nameof(HasMarkedForDeletion));
        OnPropertyChanged(nameof(ActiveMarkedForDeletionCount));
        OnPropertyChanged(nameof(HasActiveMarkedForDeletion));
        OnPropertyChanged(nameof(BulkDeleteButtonText));
        OnPropertyChanged(nameof(BulkDeleteToolTip));
        OnPropertyChanged(nameof(HasRecentClips));
        OnPropertyChanged(nameof(HasSavedClips));
        OnPropertyChanged(nameof(HasClips));
        OnPropertyChanged(nameof(HasActiveClips));

        DeleteAllOrMarkedCommand.NotifyCanExecuteChanged();
    }

    private static bool? GetSelectAllState(
        IEnumerable<ClipListItemViewModel> clips)
    {
        var list = clips.ToList();

        if (list.Count == 0 || list.All(clip => !clip.IsMarkedForDeletion))
        {
            return false;
        }

        return list.All(clip => clip.IsMarkedForDeletion) ? true : null;
    }

    private IEnumerable<ClipListItemViewModel> GetAllClipRows() =>
        RecentClips.Concat(SavedClips);

    private IEnumerable<ClipListItemViewModel> GetActiveClipRows() =>
        SelectedTabIndex == 0 ? RecentClips : SavedClips;

    private bool MatchesCurrentFilter(ClipItem clip)
    {
        var filter = CategoryName.NormalizeForStorage(FilterText);

        return filter is null ||
               (clip.Category?.Contains(filter, StringComparison.Ordinal) ?? false);
    }
}
