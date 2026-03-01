using System.Windows.Input;
using GitDesktop.Core;

namespace GitDesktop.App.ViewModels;

/// <summary>
/// Holds all state for a single open repository tab.
/// Mirrors the responsibility previously in <see cref="MainWindowViewModel"/>
/// for one repository, so that multiple repositories can be open at once.
/// </summary>
public sealed class RepositoryTabViewModel : ViewModelBase
{
    private readonly GitDesktopClient _client;
    private string _repoPath;
    private string _name;
    private ViewModelBase? _currentView;
    private string? _errorMessage;

    public RepositoryTabViewModel(GitDesktopClient client, string repoPath, string name)
    {
        _client   = client;
        _repoPath = repoPath;
        _name     = name;

        StatusVM   = new StatusViewModel(client, repoPath);
        BranchesVM = new BranchesViewModel(client, repoPath);
        HistoryVM  = new HistoryViewModel(client, repoPath);
        TagsVM     = new TagsViewModel(client, repoPath);
        RemotesVM  = new RemotesViewModel(client, repoPath);
        StashVM    = new StashViewModel(client, repoPath);
        FilesVM    = new FilesViewModel(client, repoPath);

        FetchCommand = new AsyncRelayCommand(FetchAsync);
        PullCommand  = new AsyncRelayCommand(PullAsync);
        PushCommand  = new AsyncRelayCommand(PushAsync);

        ShowStatusCommand   = new RelayCommand(() => CurrentView = StatusVM);
        ShowBranchesCommand = new RelayCommand(() => CurrentView = BranchesVM);
        ShowHistoryCommand  = new RelayCommand(() => CurrentView = HistoryVM);
        ShowTagsCommand     = new RelayCommand(() => CurrentView = TagsVM);
        ShowRemotesCommand  = new RelayCommand(() => CurrentView = RemotesVM);
        ShowStashCommand    = new RelayCommand(() => CurrentView = StashVM);
        ShowFilesCommand    = new RelayCommand(() => CurrentView = FilesVM);

        // Default to status view.
        CurrentView = StatusVM;
    }

    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>Gets the path to the repository root.</summary>
    public string RepoPath
    {
        get => _repoPath;
        private set => SetField(ref _repoPath, value);
    }

    /// <summary>Gets or sets the display name shown in the tab header.</summary>
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    /// <summary>Gets or sets the error/status message shown in the status bar.</summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    /// <summary>Gets or sets the currently displayed child view model.</summary>
    public ViewModelBase? CurrentView
    {
        get => _currentView;
        set => SetField(ref _currentView, value);
    }

    // ── Child view models ─────────────────────────────────────────────────────

    public StatusViewModel   StatusVM   { get; }
    public BranchesViewModel BranchesVM { get; }
    public HistoryViewModel  HistoryVM  { get; }
    public TagsViewModel     TagsVM     { get; }
    public RemotesViewModel  RemotesVM  { get; }
    public StashViewModel    StashVM    { get; }
    public FilesViewModel    FilesVM    { get; }

    // ── Commands ──────────────────────────────────────────────────────────────

    public ICommand FetchCommand        { get; }
    public ICommand PullCommand         { get; }
    public ICommand PushCommand         { get; }
    public ICommand ShowStatusCommand   { get; }
    public ICommand ShowBranchesCommand { get; }
    public ICommand ShowHistoryCommand  { get; }
    public ICommand ShowTagsCommand     { get; }
    public ICommand ShowRemotesCommand  { get; }
    public ICommand ShowStashCommand    { get; }
    public ICommand ShowFilesCommand    { get; }

    // ── Initial data load ─────────────────────────────────────────────────────

    /// <summary>Loads the initial data for the repository in parallel.</summary>
    public async Task LoadAsync()
    {
        ErrorMessage = null;
        await Task.WhenAll(
            StatusVM.RefreshAsync(),
            BranchesVM.RefreshAsync(),
            HistoryVM.RefreshAsync());
    }

    // ── Git operations ────────────────────────────────────────────────────────

    private async Task FetchAsync()
    {
        ErrorMessage = null;
        var result = await _client.Remote.FetchAsync(_repoPath, prune: true);
        ErrorMessage = result.Success ? "Fetch complete." : result.Error;
        if (result.Success) await StatusVM.RefreshAsync();
    }

    private async Task PullAsync()
    {
        ErrorMessage = null;
        var result = await _client.Remote.PullAsync(_repoPath);
        ErrorMessage = result.Success ? "Pull complete." : result.Error;
        if (result.Success)
            await Task.WhenAll(StatusVM.RefreshAsync(), HistoryVM.RefreshAsync());
    }

    private async Task PushAsync()
    {
        ErrorMessage = null;
        var result = await _client.Remote.PushAsync(_repoPath);
        ErrorMessage = result.Success ? "Push complete." : result.Error;
        if (result.Success) await StatusVM.RefreshAsync();
    }
}
