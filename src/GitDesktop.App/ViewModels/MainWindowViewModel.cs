using System.Windows.Input;
using GitDesktop.Core;

namespace GitDesktop.App.ViewModels;

/// <summary>
/// ViewModel for the main application window. Manages the repository path,
/// navigation between views, and top-level git operations (fetch, pull, push).
/// </summary>
public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly GitDesktopClient _client;
    private string _repoPath = string.Empty;
    private ViewModelBase? _currentView;
    private string _title = "GitDesktop";
    private string? _errorMessage;
    private bool _isRepoOpen;

    public MainWindowViewModel() : this(new GitDesktopClient()) { }

    public MainWindowViewModel(GitDesktopClient client)
    {
        _client = client;

        OpenRepositoryCommand = new AsyncRelayCommand(OpenRepositoryAsync, () => !string.IsNullOrWhiteSpace(RepoPath));
        FetchCommand = new AsyncRelayCommand(FetchAsync, () => _isRepoOpen);
        PullCommand = new AsyncRelayCommand(PullAsync, () => _isRepoOpen);
        PushCommand = new AsyncRelayCommand(PushAsync, () => _isRepoOpen);
        ShowStatusCommand = new RelayCommand(ShowStatus, () => _isRepoOpen);
        ShowBranchesCommand = new RelayCommand(ShowBranches, () => _isRepoOpen);
        ShowHistoryCommand = new RelayCommand(ShowHistory, () => _isRepoOpen);
        ShowTagsCommand = new RelayCommand(ShowTags, () => _isRepoOpen);
        ShowRemotesCommand = new RelayCommand(ShowRemotes, () => _isRepoOpen);
        ShowStashCommand = new RelayCommand(ShowStash, () => _isRepoOpen);
    }

    /// <summary>Gets or sets the path to the repository to open.</summary>
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

    /// <summary>Gets or sets the error/status message shown in the status bar.</summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetField(ref _errorMessage, value);
    }

    /// <summary>Gets the currently displayed child view model.</summary>
    public ViewModelBase? CurrentView
    {
        get => _currentView;
        private set => SetField(ref _currentView, value);
    }

    /// <summary>Gets the <see cref="StatusViewModel"/>, or <see langword="null"/> if no repo is open.</summary>
    public StatusViewModel? StatusVM { get; private set; }

    /// <summary>Gets the <see cref="BranchesViewModel"/>, or <see langword="null"/> if no repo is open.</summary>
    public BranchesViewModel? BranchesVM { get; private set; }

    /// <summary>Gets the <see cref="HistoryViewModel"/>, or <see langword="null"/> if no repo is open.</summary>
    public HistoryViewModel? HistoryVM { get; private set; }

    /// <summary>Gets the <see cref="TagsViewModel"/>, or <see langword="null"/> if no repo is open.</summary>
    public TagsViewModel? TagsVM { get; private set; }

    /// <summary>Gets the <see cref="RemotesViewModel"/>, or <see langword="null"/> if no repo is open.</summary>
    public RemotesViewModel? RemotesVM { get; private set; }

    /// <summary>Gets the <see cref="StashViewModel"/>, or <see langword="null"/> if no repo is open.</summary>
    public StashViewModel? StashVM { get; private set; }

    /// <summary>Command to open a repository from <see cref="RepoPath"/>.</summary>
    public ICommand OpenRepositoryCommand { get; }

    /// <summary>Command to fetch from all remotes.</summary>
    public ICommand FetchCommand { get; }

    /// <summary>Command to pull from upstream.</summary>
    public ICommand PullCommand { get; }

    /// <summary>Command to push to remote.</summary>
    public ICommand PushCommand { get; }

    /// <summary>Command to navigate to the Status view.</summary>
    public ICommand ShowStatusCommand { get; }

    /// <summary>Command to navigate to the Branches view.</summary>
    public ICommand ShowBranchesCommand { get; }

    /// <summary>Command to navigate to the History view.</summary>
    public ICommand ShowHistoryCommand { get; }

    /// <summary>Command to navigate to the Tags view.</summary>
    public ICommand ShowTagsCommand { get; }

    /// <summary>Command to navigate to the Remotes view.</summary>
    public ICommand ShowRemotesCommand { get; }

    /// <summary>Command to navigate to the Stash view.</summary>
    public ICommand ShowStashCommand { get; }

    /// <summary>Opens a repository at <see cref="RepoPath"/> and loads initial data.</summary>
    public async Task OpenRepositoryAsync()
    {
        ErrorMessage = null;
        var repo = await _client.Repository.OpenAsync(RepoPath);
        if (repo == null)
        {
            ErrorMessage = $"Not a git repository: {RepoPath}";
            return;
        }

        _isRepoOpen = true;
        Title = $"GitDesktop — {repo.Path}";

        StatusVM = new StatusViewModel(_client, RepoPath);
        BranchesVM = new BranchesViewModel(_client, RepoPath);
        HistoryVM = new HistoryViewModel(_client, RepoPath);
        TagsVM = new TagsViewModel(_client, RepoPath);
        RemotesVM = new RemotesViewModel(_client, RepoPath);
        StashVM = new StashViewModel(_client, RepoPath);

        OnPropertyChanged(nameof(StatusVM));
        OnPropertyChanged(nameof(BranchesVM));
        OnPropertyChanged(nameof(HistoryVM));
        OnPropertyChanged(nameof(TagsVM));
        OnPropertyChanged(nameof(RemotesVM));
        OnPropertyChanged(nameof(StashVM));

        ((AsyncRelayCommand)FetchCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)PullCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)PushCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ShowStatusCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ShowBranchesCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ShowHistoryCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ShowTagsCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ShowRemotesCommand).RaiseCanExecuteChanged();
        ((RelayCommand)ShowStashCommand).RaiseCanExecuteChanged();

        // Start on status view and load data in parallel
        ShowStatus();
        await Task.WhenAll(
            StatusVM.RefreshAsync(),
            BranchesVM.RefreshAsync(),
            HistoryVM.RefreshAsync());
    }

    private void ShowStatus()
    {
        if (StatusVM != null) CurrentView = StatusVM;
    }

    private void ShowBranches()
    {
        if (BranchesVM != null) CurrentView = BranchesVM;
    }

    private void ShowHistory()
    {
        if (HistoryVM != null) CurrentView = HistoryVM;
    }

    private void ShowTags()
    {
        if (TagsVM != null) CurrentView = TagsVM;
    }

    private void ShowRemotes()
    {
        if (RemotesVM != null) CurrentView = RemotesVM;
    }

    private void ShowStash()
    {
        if (StashVM != null) CurrentView = StashVM;
    }

    private async Task FetchAsync()
    {
        ErrorMessage = null;
        var result = await _client.Remote.FetchAsync(RepoPath, prune: true);
        ErrorMessage = result.Success ? "Fetch complete." : result.Error;
        if (result.Success && StatusVM != null)
            await StatusVM.RefreshAsync();
    }

    private async Task PullAsync()
    {
        ErrorMessage = null;
        var result = await _client.Remote.PullAsync(RepoPath);
        ErrorMessage = result.Success ? "Pull complete." : result.Error;
        if (result.Success && StatusVM != null)
            await Task.WhenAll(StatusVM.RefreshAsync(), HistoryVM?.RefreshAsync() ?? Task.CompletedTask);
    }

    private async Task PushAsync()
    {
        ErrorMessage = null;
        var result = await _client.Remote.PushAsync(RepoPath);
        ErrorMessage = result.Success ? "Push complete." : result.Error;
        if (result.Success && StatusVM != null)
            await StatusVM.RefreshAsync();
    }
}
