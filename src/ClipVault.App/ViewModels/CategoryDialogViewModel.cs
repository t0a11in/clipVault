using System.Collections.ObjectModel;
using ClipVault.Core.Interfaces;
using ClipVault.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClipVault.App.ViewModels;

public partial class CategoryDialogViewModel(IClipRepository repository) : ObservableObject
{
    public ObservableCollection<CategoryOption> Categories { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCategoryCommand))]
    private string newCategory = string.Empty;

    [ObservableProperty]
    private string status = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateCategoryCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCategoryCommand))]
    private CategoryOption? selectedCategory;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateCategoryCommand))]
    private string editedCategory = string.Empty;

    public bool CanDeleteSelectedCategory => SelectedCategory?.IsUserCreated == true;

    public Action? CloseRequested { get; set; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var categories = await repository.GetCategoryItemsAsync(cancellationToken);
        Categories.Clear();

        foreach (var category in categories)
        {
            Categories.Add(new CategoryOption(category.Name, category.IsUserCreated));
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddCategory))]
    private async Task AddCategoryAsync()
    {
        var normalized = CategoryName.NormalizeForStorage(NewCategory);

        if (normalized is null)
        {
            return;
        }

        var alreadyExists = Categories.Any(category =>
            string.Equals(category.StorageValue, normalized, StringComparison.Ordinal));

        await repository.AddCategoryAsync(normalized);

        if (!alreadyExists)
        {
            Categories.Add(new CategoryOption(normalized, IsUserCreated: true));
            Status = $"Categoría {CategoryName.ToDisplay(normalized)} agregada.";
        }
        else
        {
            Status = "La categoría ya existe.";
        }

        NewCategory = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanUpdateCategory))]
    private async Task UpdateCategoryAsync()
    {
        if (SelectedCategory is null)
        {
            return;
        }

        var current = SelectedCategory.StorageValue;
        var updated = CategoryName.NormalizeForStorage(EditedCategory);

        if (updated is null)
        {
            return;
        }

        if (!await repository.UpdateCategoryAsync(current, updated))
        {
            Status = "No se pudo editar: la categoría no existe o el nuevo nombre está repetido.";
            return;
        }

        await InitializeAsync();
        SelectedCategory = Categories.FirstOrDefault(category => category.StorageValue == updated);
        Status = "Categoría actualizada.";
    }

    [RelayCommand(CanExecute = nameof(CanDeleteCategory))]
    private async Task DeleteCategoryAsync()
    {
        if (SelectedCategory is null)
        {
            return;
        }

        var category = SelectedCategory;

        if (!await repository.DeleteUserCategoryAsync(category.StorageValue))
        {
            Status = "Solo se pueden borrar categorías creadas desde ClipVault.";
            return;
        }

        await InitializeAsync();
        Status = $"Categoría {category.DisplayName} eliminada; sus clips quedaron sin categoría.";
    }

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke();
    }

    private bool CanAddCategory() => !string.IsNullOrWhiteSpace(NewCategory);

    private bool CanUpdateCategory() =>
        SelectedCategory is not null && !string.IsNullOrWhiteSpace(EditedCategory);

    private bool CanDeleteCategory() => CanDeleteSelectedCategory;

    partial void OnSelectedCategoryChanged(CategoryOption? value)
    {
        EditedCategory = value?.DisplayName ?? string.Empty;
        OnPropertyChanged(nameof(CanDeleteSelectedCategory));
        DeleteCategoryCommand.NotifyCanExecuteChanged();
    }
}
