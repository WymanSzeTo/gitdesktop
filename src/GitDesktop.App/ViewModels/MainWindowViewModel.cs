using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
    private string _customName = string.Empty;
    private string _selectedTabName = string.Empty;
    private string _title = "GitDesktop";
    private string? _globalErrorMessage;
    private RepositoryTabViewModel? _selectedTab;

    // Backing collection for the saved-repositories sidebar.
    // Using ObservableCollection so Avalonia's ListBox receives INotifyCollectionChanged
    // notifications whenever entries are added, removed, or renamed, avoiding the stale-
    // last-item display bug that occurred when the list was mutated before PropertyChanged fired.
    private readonly ObservableCollection<RepositoryEntry> _knownRepositories = [];

    // Prevents session saves from firing during the initial startup restore.
    private bool _sessionReady;

    // ── Theme / font ─────────────────────────────────────────────────────────
    private string _selectedTheme = "Dark";
    private double _fontSize = 13.0;

    public MainWindowViewModel() : this(new GitDesktopClient(), new AppConfigService()) { }

    public MainWindowViewModel(GitDesktopClient client, AppConfigService? configService = null)
    {
        _client        = client;
        _configService = configService ?? new AppConfigService();

        Tabs = [];

        OpenRepositoryCommand          = new AsyncRelayCommand(OpenRepositoryAsync, () => !string.IsNullOrWhiteSpace(RepoPath));
        AddRepositoryCommand           = new AsyncRelayCommand<string>(path => AddRepositoryByPathAsync(path));
        RemoveSavedRepositoryCommand   = new AsyncRelayCommand<RepositoryEntry>(RemoveSavedRepositoryAsync);
        CloseTabCommand                = new RelayCommand<RepositoryTabViewModel>(CloseTab);
        SaveSettingsCommand            = new AsyncRelayCommand(SaveSettingsAsync);
        LoadConfigCommand              = new AsyncRelayCommand(LoadConfigAsync);
        SaveTabNameCommand             = new AsyncRelayCommand(SaveTabNameAsync);
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

    /// <summary>
    /// Gets or sets the optional display name entered by the user before opening a repository.
    /// When non-empty, the new tab will use this name; otherwise the folder name is used.
    /// </summary>
    public string CustomName
    {
        get => _customName;
        set => SetField(ref _customName, value);
    }

    /// <summary>
    /// Gets or sets the editable display name of the currently selected tab.
    /// Changing this value does NOT persist immediately; call <see cref="SaveTabNameCommand"/>
    /// (or press the Rename button in the UI) to persist the change.
    /// </summary>
    public string SelectedTabName
    {
        get => _selectedTabName;
        set => SetField(ref _selectedTabName, value);
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
            var oldTab = _selectedTab;
            if (SetField(ref _selectedTab, value))
            {
                // Forward property change notifications from the active tab to this view model
                // so that the toolbar and status bar update live during git operations.
                if (oldTab != null)
                    oldTab.PropertyChanged -= OnSelectedTabPropertyChanged;
                if (value != null)
                    value.PropertyChanged += OnSelectedTabPropertyChanged;

                Title = value != null ? $"GitDesktop — {value.RepoPath}" : "GitDesktop";

                // Sync the editable name field to the newly selected tab.
                SelectedTabName = value?.Name ?? string.Empty;

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
                OnPropertyChanged(nameof(IsOperationInProgress));
                OnPropertyChanged(nameof(OperationStatus));

                // Notify view-navigation commands so the sidebar buttons re-evaluate.
                OnPropertyChanged(nameof(ShowStatusCommand));
                OnPropertyChanged(nameof(ShowBranchesCommand));
                OnPropertyChanged(nameof(ShowHistoryCommand));
                OnPropertyChanged(nameof(ShowTagsCommand));
                OnPropertyChanged(nameof(ShowRemotesCommand));
                OnPropertyChanged(nameof(ShowStashCommand));
                OnPropertyChanged(nameof(ShowFilesCommand));

                _ = SaveSessionAsync();
            }
        }
    }

    /// <summary>Gets the list of known repositories from the config (for the sidebar).
    /// Backed by an <see cref="ObservableCollection{T}"/> so that Avalonia's ListBox
    /// receives <see cref="System.Collections.Specialized.INotifyCollectionChanged"/>
    /// notifications and always reflects the current state.</summary>
    public IReadOnlyList<RepositoryEntry> KnownRepositories => _knownRepositories;

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

    /// <summary>Gets a value indicating whether the active tab has a git operation in progress.</summary>
    public bool IsOperationInProgress => _selectedTab?.IsOperationInProgress ?? false;

    /// <summary>Gets the current operation status message from the active tab.</summary>
    public string? OperationStatus => _selectedTab?.OperationStatus;

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

    /// <summary>Removes a repository entry from the saved list.</summary>
    public ICommand RemoveSavedRepositoryCommand { get; }

    /// <summary>Closes a repository tab.</summary>
    public ICommand CloseTabCommand { get; }

    /// <summary>Persists current settings (theme, font size) to disk.</summary>
    public ICommand SaveSettingsCommand { get; }

    /// <summary>Reloads the config from disk.</summary>
    public ICommand LoadConfigCommand { get; }

    /// <summary>Renames the currently selected tab using <see cref="SelectedTabName"/> and persists the change.</summary>
    public ICommand SaveTabNameCommand { get; }

    // ── Implementation ────────────────────────────────────────────────────────

    /// <summary>
    /// Opens a repository at <see cref="RepoPath"/>, optionally using <see cref="CustomName"/>
    /// as the display name, and creates a new tab (or switches to the existing one).
    /// </summary>
    public async Task OpenRepositoryAsync()
    {
        var explicitName = string.IsNullOrWhiteSpace(CustomName) ? null : CustomName;
        await AddRepositoryByPathAsync(RepoPath, explicitName);
        // Clear the custom-name field only when we successfully provided one.
        if (explicitName is not null)
            CustomName = string.Empty;
    }

    /// <summary>
    /// Loads the application configuration and then re-opens all repositories that were
    /// open in the previous session, restoring the previously active tab.
    /// Called once on application startup.
    /// </summary>
    public async Task StartupAsync()
    {
        await LoadConfigAsync();

        foreach (var path in _config.OpenRepositoryPaths.ToList())
        {
            await AddRepositoryByPathAsync(path);
        }

        // Restore the previously active tab.
        if (_config.SelectedRepositoryPath is not null)
        {
            var norm = Path.GetFullPath(_config.SelectedRepositoryPath);
            var tab  = Tabs.FirstOrDefault(t => Path.GetFullPath(t.RepoPath) == norm);
            if (tab != null)
                SelectedTab = tab;
        }

        _sessionReady = true;
    }

    private async Task AddRepositoryByPathAsync(string? path, string? explicitName = null)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        var normalised = Path.GetFullPath(path);

        // Do not open the same repository more than once — switch to the existing tab instead.
        var existing = Tabs.FirstOrDefault(t => Path.GetFullPath(t.RepoPath) == normalised);
        if (existing != null)
        {
            SelectedTab = existing;
            return;
        }

        var repo = await _client.Repository.OpenAsync(path);
        if (repo == null)
        {
            ErrorMessage = $"Not a git repository: {path}";
            return;
        }

        // Determine display name: explicit > stored config > folder name.
        var storedEntry = _config.Repositories.FirstOrDefault(r => Path.GetFullPath(r.Path) == normalised);
        var name = explicitName
            ?? storedEntry?.Name
            ?? Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar));

        var tab = new RepositoryTabViewModel(_client, path, name);
        Tabs.Add(tab);
        SelectedTab = tab;

        // Persist to config (also saves the session).
        await _configService.AddRepositoryAsync(_config, normalised, name);

        // Sync the observable sidebar collection if the entry was freshly added to config.
        var addedEntry = _config.Repositories.FirstOrDefault(r => Path.GetFullPath(r.Path) == normalised);
        if (addedEntry != null && !_knownRepositories.Contains(addedEntry))
            _knownRepositories.Add(addedEntry);

        await SaveSessionAsync();

        // Load data for the new tab.
        await tab.LoadAsync();
    }

    /// <summary>
    /// Removes a saved repository entry by path. Exposed as <see langword="public"/>
    /// so callers (e.g. tests) can await it directly.
    /// Also closes any open tab for that repository immediately.
    /// </summary>
    public async Task RemoveSavedRepositoryAsync(RepositoryEntry? entry)
    {
        if (entry == null) return;

        // Close the open tab for this repository (if any) so all UI state is removed immediately.
        var normalised = Path.GetFullPath(entry.Path);
        var openTab = Tabs.FirstOrDefault(t => Path.GetFullPath(t.RepoPath) == normalised);
        if (openTab != null)
            CloseTab(openTab);

        await _configService.RemoveRepositoryAsync(_config, entry.Path);

        // Remove from the observable sidebar collection so the ListBox updates immediately.
        var toRemove = _knownRepositories.FirstOrDefault(r => Path.GetFullPath(r.Path) == normalised);
        if (toRemove != null)
            _knownRepositories.Remove(toRemove);
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

        _ = SaveSessionAsync();
    }

    /// <summary>
    /// Applies <see cref="SelectedTabName"/> to the current tab and persists the change.
    /// Exposed as public so it can be awaited directly in tests.
    /// </summary>
    public async Task SaveTabNameAsync()
    {
        if (_selectedTab is null || string.IsNullOrWhiteSpace(_selectedTabName)) return;
        _selectedTab.Name = _selectedTabName;
        await _configService.UpdateRepositoryNameAsync(_config, _selectedTab.RepoPath, _selectedTabName);

        // Keep the observable sidebar collection in sync with the renamed entry.
        var normalised = Path.GetFullPath(_selectedTab.RepoPath);
        var sidebarEntry = _knownRepositories.FirstOrDefault(r => Path.GetFullPath(r.Path) == normalised);
        if (sidebarEntry != null)
            sidebarEntry.Name = _selectedTabName;
    }

    private async Task SaveSettingsAsync()
    {
        _config.Theme    = _selectedTheme;
        _config.FontSize = _fontSize;
        await _configService.SaveAsync(_config);
    }

    /// <summary>
    /// Forwards selected tab's property change events to this view model so the toolbar
    /// and status bar update live during Fetch, Pull, Push, and Commit operations.
    /// </summary>
    private void OnSelectedTabPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(RepositoryTabViewModel.IsOperationInProgress):
                OnPropertyChanged(nameof(IsOperationInProgress));
                break;
            case nameof(RepositoryTabViewModel.OperationStatus):
                OnPropertyChanged(nameof(OperationStatus));
                break;
            case nameof(RepositoryTabViewModel.ErrorMessage):
                OnPropertyChanged(nameof(ErrorMessage));
                break;
        }
    }

    /// <summary>Loads the configuration from disk. Exposed for testing.</summary>
    public async Task LoadConfigAsync()
    {
        _config = await _configService.LoadAsync();

        _knownRepositories.Clear();
        foreach (var entry in _config.Repositories)
            _knownRepositories.Add(entry);

        _selectedTheme = _config.Theme;
        _fontSize      = _config.FontSize > 0 ? _config.FontSize : 13.0;
        OnPropertyChanged(nameof(SelectedTheme));
        OnPropertyChanged(nameof(FontSize));

        ThemeManager.ApplyTheme(_selectedTheme);
        ApplyFontSize(_fontSize);
    }

    /// <summary>
    /// Persists the list of currently open tab paths and the selected path to disk.
    /// No-op during the initial startup restore to avoid overwriting the saved session.
    /// </summary>
    private Task SaveSessionAsync()
    {
        if (!_sessionReady) return Task.CompletedTask;
        return _configService.UpdateOpenSessionAsync(
            _config,
            Tabs.Select(t => t.RepoPath),
            _selectedTab?.RepoPath);
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
