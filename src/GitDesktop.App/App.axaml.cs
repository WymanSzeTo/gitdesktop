using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GitDesktop.App.Services;
using GitDesktop.App.Views;

namespace GitDesktop.App;

/// <summary>
/// Avalonia application entry point. Initialises the Fluent theme and
/// creates the main window on startup.
/// </summary>
public sealed class App : Application
{
    /// <inheritdoc />
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Apply theme from persisted config before showing the window.
            _ = ApplyStoredThemeAsync();
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task ApplyStoredThemeAsync()
    {
        try
        {
            var svc    = new AppConfigService();
            var config = await svc.LoadAsync();
            ThemeManager.ApplyTheme(config.Theme);
        }
        catch
        {
            // If loading fails, the defaults defined in App.axaml remain in effect.
        }
    }
}
