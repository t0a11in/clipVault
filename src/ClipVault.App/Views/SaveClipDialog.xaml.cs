using System.Windows;
using System.Windows.Input;
using ClipVault.App.ViewModels;

namespace ClipVault.App.Views;

public partial class SaveClipDialog : Window
{
    private readonly SaveClipDialogViewModel _viewModel;

    public SaveClipDialog(SaveClipDialogViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        viewModel.CloseRequested = result => DialogResult = result;
        DataContext = viewModel;
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
