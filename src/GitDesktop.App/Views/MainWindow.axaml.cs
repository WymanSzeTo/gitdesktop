using Avalonia.Controls;
using GitDesktop.App.ViewModels;

namespace GitDesktop.App.Views;

/// <summary>
/// The application's main window. Hosts the navigation sidebar, top toolbar,
/// and the content area that swaps between Status, Branches, and History views.
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>Creates the window with the default <see cref="MainWindowViewModel"/>.</summary>
    public MainWindow() : this(new MainWindowViewModel()) { }

    /// <summary>Creates the window with an explicitly provided <paramref name="viewModel"/>.</summary>
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
