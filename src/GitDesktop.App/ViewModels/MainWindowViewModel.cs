using System.Collections.ObjectModel;
using System.Windows.Input;
using GitDesktop.App.Models;
using GitDesktop.App.Services;
using GitDesktop.Core;

namespace GitDesktop.App.ViewModels;

/// <summary>
/// ViewModel for the main application window. Manages the collection of open
/// repository tabs, global settings (theme, font size), and exposes
/// pass-through properties for backward compatibility.
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly GitDesktopClient _client;
    private readonly AppConfigService _configService;
    private AppConfig _config = new();

    private string _repoPath = string.Empty;
    private string _title = "GitDesktop";
    private string? _globalErrorMessage;
    private RepositoryTabViewModel? _selectedTab;

    // ── Theme / font ─────────────────────────────────────────────────────────
    private string _selectedTheme = "Dark";
    private double _fontSize = 13.0;

    public MainWindowViewModel() : this(new GitDesktopClient(), new AppConfigService()) { }

    public MainWindowViewModel(GitDesktopClient client, AppConfigService? configService = null)
    {
        _client        = client;
        _configService = configService ?? new AppConfigService();

        Tabs = [];

        OpenRepositoryCommand = new AsyncRelayCommand(OpenRepositoryAsync, () => !string.IsNullOrWhiteSpace(RepoPath));
        AddRepositoryCommand  = new AsyncRelayCommand<string>(AddRepositoryByPathAsync);
        CloseTabCommand       = new RelayCommand<RepositoryTabViewModel>(CloseTab);
        SaveSettingsCommand   = new AsyncRelayCommand(SaveSettingsAsync);
        LoadConfigCommand     = new AsyncRelayCommand(LoadConfigAsync);
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>Gets or sets the path used to open a new repository tab.</summary>
    public string RepoPath
    {
        get => _repoPath;
        set
        {
            SetField(ref _repoPath, value);
            ((AsyncRelayCommand)OpenRepositoryCommand).RaiseCanExecuteChanged();
        }
    }

    /// <summary>Gets the title displayed in the window title bar.</summary>
    public string Title
    {
        get => _title;
        private set => SetField(ref _title, value);
    }

    /// <summary>Gets the collection of open repository tabs.</summary>
    public ObservableCollection<RepositoryTabViewModel> Tabs { get; }

    /// <summary>Gets or sets the currently selected repository tab.</summary>
    public RepositoryTabViewModel? SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (SetField(ref _selectedTab, value))
            {
                Title = value != null ? $"GitDesktop — {value.RepoPath}" : "GitDesktop";
                OnPropertyChanged(nameof(ErrorMessage));
                OnPropertyChanged(nameof(CurrentView));
                OnPropertyChanged(nameof(StatusVM));
                OnPropertyChanged(nameof(BranchesVM));
                OnPropertyChanged(nameof(HistoryVM));
                OnPropertyChanged(nameof(TagsVM));
                OnPropertyChanged(nameof(RemotesVM));
                OnPropertyChanged(nameof(StashVM));
                OnPropertyChanged(nameof(FilesVM));
                OnPropertyChanged(nameof(FetchCommand));
                OnPropertyChanged(nameof(PullCommand));
                OnPropertyChanged(nameof(PushCommand));
            }
        }
    }

    /// <summary>Gets the list of known repositories from the config (for the sidebar).</summary>
    public IReadOnlyList<RepositoryEntry> KnownRepositories => _config.Repositories;

    // ── Pass-through properties (delegates to SelectedTab) ───────────────────

    /// <summary>Gets the error/status message for the active tab (or a global error).</summary>
    public string? ErrorMessage
    {
        get => _selectedTab?.ErrorMessage ?? _globalErrorMessage;
        private set
        {
            _globalErrorMessage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets the currently displayed child view model in the active tab.</summary>
    public ViewModelBase? CurrentView => _selectedTab?.CurrentView;

    public StatusViewModel?   StatusVM   => _selectedTab?.StatusVM;
    public BranchesViewModel? BranchesVM => _selectedTab?.BranchesVM;
    public HistoryViewModel?  HistoryVM  => _selectedTab?.HistoryVM;
    public TagsViewModel?     TagsVM     => _selectedTab?.TagsVM;
    public RemotesViewModel?  RemotesVM  => _selectedTab?.RemotesVM;
    public StashViewModel?    StashVM    => _selectedTab?.StashVM;
    public FilesViewModel?    FilesVM    => _selectedTab?.FilesVM;

    // ── Pass-through commands (delegates to SelectedTab) ─────────────────────

    public ICommand? FetchCommand        => _selectedTab?.FetchCommand;
    public ICommand? PullCommand         => _selectedTab?.PullCommand;
    public ICommand? PushCommand         => _selectedTab?.PushCommand;
    public ICommand? ShowStatusCommand   => _selectedTab?.ShowStatusCommand;
    public ICommand? ShowBranchesCommand => _selectedTab?.ShowBranchesCommand;
    public ICommand? ShowHistoryCommand  => _selectedTab?.ShowHistoryCommand;
    public ICommand? ShowTagsCommand     => _selectedTab?.ShowTagsCommand;
    public ICommand? ShowRemotesCommand  => _selectedTab?.ShowRemotesCommand;
    public ICommand? ShowStashCommand    => _selectedTab?.ShowStashCommand;
    public ICommand? ShowFilesCommand    => _selectedTab?.ShowFilesCommand;

    // ── Theme & font size ─────────────────────────────────────────────────────

    /// <summary>Gets the ordered list of available theme names.</summary>
    public IReadOnlyList<string> AvailableThemes => ThemeManager.ThemeNames;

    /// <summary>Gets or sets the name of the active colour theme.</summary>
    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetField(ref _selectedTheme, value))
            {
                ThemeManager.ApplyTheme(value);
                _config.Theme = value;
            }
        }
    }

    /// <summary>Gets or sets the global UI font size.</summary>
    public double FontSize
    {
        get => _fontSize;
        set
        {
            if (SetField(ref _fontSize, value))
            {
                ApplyFontSize(value);
                _config.FontSize = value;
            }
        }
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    /// <summary>Opens a new repository tab for <see cref="RepoPath"/>.</summary>
    public ICommand OpenRepositoryCommand { get; }

    /// <summary>Opens a repository tab by path (used from the saved-repos list).</summary>
    public ICommand AddRepositoryCommand { get; }

    /// <summary>Closes a repository tab.</summary>
    public ICommand CloseTabCommand { get; }

    /// <summary>Persists current settings (theme, font size) to disk.</summary>
    public ICommand SaveSettingsCommand { get; }

    /// <summary>Reloads the config from disk.</summary>
    public ICommand LoadConfigCommand { get; }

    // ── Implementation ────────────────────────────────────────────────────────

    /// <summary>Opens a repository at <see cref="RepoPath"/> and creates a new tab.</summary>
    public async Task OpenRepositoryAsync()
    {
        await AddRepositoryByPathAsync(RepoPath);
    }

    private async Task AddRepositoryByPathAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        var repo = await _client.Repository.OpenAsync(path);
        if (repo == null)
        {
            ErrorMessage = $"Not a git repository: {path}";
            return;
        }

        var name = System.IO.Path.GetFileName(path.TrimEnd(System.IO.Path.DirectorySeparatorChar));
        var tab  = new RepositoryTabViewModel(_client, path, name);
        Tabs.Add(tab);
        SelectedTab = tab;

        // Persist to config.
        await _configService.AddRepositoryAsync(_config, path, name);
        OnPropertyChanged(nameof(KnownRepositories));

        // Load data for the new tab.
        await tab.LoadAsync();
    }

    private void CloseTab(RepositoryTabViewModel? tab)
    {
        if (tab == null) return;
        var idx = Tabs.IndexOf(tab);
        Tabs.Remove(tab);

        if (Tabs.Count == 0)
        {
            SelectedTab = null;
        }
        else
        {
            var newIdx = Math.Max(0, Math.Min(idx, Tabs.Count - 1));
            SelectedTab = Tabs[newIdx];
        }
    }

    private async Task SaveSettingsAsync()
    {
        _config.Theme    = _selectedTheme;
        _config.FontSize = _fontSize;
        await _configService.SaveAsync(_config);
    }

    /// <summary>Loads the configuration from disk. Exposed for testing.</summary>
    public async Task LoadConfigAsync()
    {
        _config = await _configService.LoadAsync();
        OnPropertyChanged(nameof(KnownRepositories));

        _selectedTheme = _config.Theme;
        _fontSize      = _config.FontSize > 0 ? _config.FontSize : 13.0;
        OnPropertyChanged(nameof(SelectedTheme));
        OnPropertyChanged(nameof(FontSize));

        ThemeManager.ApplyTheme(_selectedTheme);
        ApplyFontSize(_fontSize);
    }

    private static void ApplyFontSize(double size)
    {
        var app = Avalonia.Application.Current;
        if (app == null) return;
        app.Resources["AppFontSize"]        = size;
        app.Resources["AppFontSizeLarge"]   = size + 2;
        app.Resources["AppFontSizeSmall"]   = Math.Max(9, size - 2);
        app.Resources["AppFontSizeHeading"] = size + 4;
    }
}
