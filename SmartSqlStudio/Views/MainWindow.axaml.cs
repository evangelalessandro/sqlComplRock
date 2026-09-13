using Avalonia.Controls;
using Avalonia.Interactivity;
using SmartSqlStudio.ViewModels;

namespace SmartSqlStudio.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnInsertSnippetClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel && 
            sender is Button button && 
            button.DataContext is Models.SqlSnippet snippet)
        {
            viewModel.SqlQuery += Environment.NewLine + snippet.Content;
        }
    }
}
