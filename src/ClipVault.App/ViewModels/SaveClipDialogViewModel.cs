using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using ClipVault.Core.Models;

namespace ClipVault.App.ViewModels;

public partial class SaveClipDialogViewModel : ObservableObject
{
    public ObservableCollection<CategoryOption> Categories { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string title = string.Empty;

    [ObservableProperty]
    private string category = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCategoryCommand))]
    private string newCategory = string.Empty;

    [ObservableProperty]
    private bool isAddingCategory;

    public Action<bool?>? CloseRequested { get; set; }

    public void SetCategories(IEnumerable<string> categories)
    {
        Categories.Clear();

        foreach (var category in categories
                     .Select(CategoryName.NormalizeForStorage)
                     .Where(category => category is not null)
                     .Cast<string>()
                     .Distinct(StringComparer.Ordinal))
        {
            Categories.Add(new CategoryOption(category));
        }
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Title);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(false);
    }

    [RelayCommand]
    private void BeginAddCategory()
    {
        NewCategory = string.Empty;
        IsAddingCategory = true;
    }

    [RelayCommand(CanExecute = nameof(CanAddCategory))]
    private void AddCategory()
    {
        var value = CategoryName.NormalizeForStorage(NewCategory)!;
        var existing = Categories.FirstOrDefault(category =>
            string.Equals(category.StorageValue, value, StringComparison.Ordinal));

        if (existing is null)
        {
            existing = new CategoryOption(value);
            Categories.Add(existing);
        }

        // El ComboBox sigue siendo un selector: la nueva categoría se elige aquí,
        // desde fuera del control, y se guardará al confirmar el clip.
        Category = existing.StorageValue;
        NewCategory = string.Empty;
        IsAddingCategory = false;
    }

    [RelayCommand]
    private void CancelAddCategory()
    {
        NewCategory = string.Empty;
        IsAddingCategory = false;
    }

    private bool CanAddCategory() => !string.IsNullOrWhiteSpace(NewCategory);
}
