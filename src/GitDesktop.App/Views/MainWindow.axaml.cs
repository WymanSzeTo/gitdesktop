using Avalonia.Controls;
using GitDesktop.App.ViewModels;

namespace GitDesktop.App.Views;

/// <summary>
/// The application's main window. Hosts the navigation sidebar, top toolbar,
/// and the content area that swaps between Status, Branches, and History views.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
