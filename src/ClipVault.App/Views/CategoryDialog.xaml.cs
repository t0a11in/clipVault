using System.Windows;
using System.Windows.Input;
using ClipVault.App.ViewModels;

namespace ClipVault.App.Views;

public partial class CategoryDialog : Window
{
    private readonly CategoryDialogViewModel _viewModel;

    public CategoryDialog(CategoryDialogViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.CloseRequested = Close;
    }

    private void OnNewCategoryKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || !_viewModel.AddCategoryCommand.CanExecute(null))
        {
            return;
        }

        _viewModel.AddCategoryCommand.Execute(null);
        e.Handled = true;
    }
}
