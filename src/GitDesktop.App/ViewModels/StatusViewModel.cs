using System.Collections.ObjectModel;
using System.Windows.Input;
using GitDesktop.Core;
using GitDesktop.Core.Models;

namespace GitDesktop.App.ViewModels;

/// <summary>
/// ViewModel for the repository status view. Shows staged, unstaged, and
/// untracked files, and supports staging/unstaging individual files.
/// </summary>
public sealed class StatusViewModel : ViewModelBase
{
    private readonly GitDesktopClient _client;
    private string _repoPath;
    private string _currentBranch = string.Empty;
    private string? _upstreamBranch;
    private int _aheadCount;
    private int _behindCount;
    private bool _isLoading;
    private string _commitMessage = string.Empty;
    private string? _statusMessage;

    private bool _amendMode;

    public StatusViewModel(GitDesktopClient client, string repoPath)
    {
        _client = client;
        _repoPath = repoPath;

        StagedFiles = [];
        UnstagedFiles = [];
        UntrackedFiles = [];

        StageFileCommand = new AsyncRelayCommand<StatusEntry>(StageFileAsync);
        UnstageFileCommand = new AsyncRelayCommand<StatusEntry>(UnstageFileAsync);
        StageAllCommand = new AsyncRelayCommand(StageAllAsync);
        CommitCommand = new AsyncRelayCommand(CommitAsync, CanCommit);
        DiscardFileCommand = new AsyncRelayCommand<StatusEntry>(DiscardFileAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    /// <summary>Gets the currently checked-out branch name.</summary>
    public string CurrentBranch
    {
        get => _currentBranch;
        private set => SetField(ref _currentBranch, value);
    }

    /// <summary>Gets the upstream tracking branch name, or <see langword="null"/> if none.</summary>
    public string? UpstreamBranch
    {
        get => _upstreamBranch;
        private set => SetField(ref _upstreamBranch, value);
    }

    /// <summary>Gets the number of commits ahead of upstream.</summary>
    public int AheadCount
    {
        get => _aheadCount;
        private set => SetField(ref _aheadCount, value);
    }

    /// <summary>Gets the number of commits behind upstream.</summary>
    public int BehindCount
    {
        get => _behindCount;
        private set => SetField(ref _behindCount, value);
    }

    /// <summary>Gets or sets a value indicating whether the view is loading data.</summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    /// <summary>Gets or sets the commit message to use when committing.</summary>
    public string CommitMessage
    {
        get => _commitMessage;
        set
        {
            SetField(ref _commitMessage, value);
            ((AsyncRelayCommand)CommitCommand).RaiseCanExecuteChanged();
        }
    }

    /// <summary>Gets or sets a transient status message shown to the user.</summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    /// <summary>Gets the list of staged (indexed) files.</summary>
    public ObservableCollection<StatusEntry> StagedFiles { get; }

    /// <summary>Gets the list of unstaged (modified/deleted) files.</summary>
    public ObservableCollection<StatusEntry> UnstagedFiles { get; }

    /// <summary>Gets the list of untracked files.</summary>
    public ObservableCollection<StatusEntry> UntrackedFiles { get; }

    /// <summary>Command to stage a single file.</summary>
    public ICommand StageFileCommand { get; }

    /// <summary>Command to unstage a single file.</summary>
    public ICommand UnstageFileCommand { get; }

    /// <summary>Command to stage all changes.</summary>
    public ICommand StageAllCommand { get; }

    /// <summary>Command to create a commit with the current staged files and commit message.</summary>
    public ICommand CommitCommand { get; }

    /// <summary>Command to discard changes in a working-tree file.</summary>
    public ICommand DiscardFileCommand { get; }

    /// <summary>Gets or sets a value indicating whether the next commit should amend the last one.</summary>
    public bool AmendMode
    {
        get => _amendMode;
        set
        {
            SetField(ref _amendMode, value);
            ((AsyncRelayCommand)CommitCommand).RaiseCanExecuteChanged();
        }
    }

    /// <summary>Command to refresh the status.</summary>
    public ICommand RefreshCommand { get; }

    /// <summary>Loads the repository status asynchronously.</summary>
    public async Task RefreshAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var status = await _client.Repository.GetStatusAsync(_repoPath);
            CurrentBranch = status.CurrentBranch;
            UpstreamBranch = status.UpstreamBranch;
            AheadCount = status.AheadCount;
            BehindCount = status.BehindCount;

            StagedFiles.Clear();
            foreach (var e in status.StagedEntries) StagedFiles.Add(e);

            UnstagedFiles.Clear();
            foreach (var e in status.UnstagedEntries) UnstagedFiles.Add(e);

            UntrackedFiles.Clear();
            foreach (var e in status.UntrackedEntries) UntrackedFiles.Add(e);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task StageFileAsync(StatusEntry? entry)
    {
        if (entry == null) return;
        var result = await _client.Commit.StageAsync(_repoPath, entry.Path);
        StatusMessage = result.Success ? $"Staged: {entry.Path}" : result.Error;
        await RefreshAsync();
    }

    private async Task UnstageFileAsync(StatusEntry? entry)
    {
        if (entry == null) return;
        var result = await _client.Commit.UnstageAsync(_repoPath, entry.Path);
        StatusMessage = result.Success ? $"Unstaged: {entry.Path}" : result.Error;
        await RefreshAsync();
    }

    private async Task StageAllAsync()
    {
        var result = await _client.Commit.StageAllAsync(_repoPath);
        StatusMessage = result.Success ? "All changes staged." : result.Error;
        await RefreshAsync();
    }

    private async Task CommitAsync()
    {
        if (string.IsNullOrWhiteSpace(CommitMessage) && !AmendMode) return;

        GitDesktop.Core.Execution.GitResult result;
        if (AmendMode)
        {
            var msg = string.IsNullOrWhiteSpace(CommitMessage) ? null : CommitMessage;
            result = await _client.Commit.AmendAsync(_repoPath, msg, noEdit: msg == null);
        }
        else
        {
            result = await _client.Commit.CommitAsync(_repoPath, CommitMessage);
        }

        if (result.Success)
        {
            var wasAmend = AmendMode;
            CommitMessage = string.Empty;
            AmendMode = false;
            StatusMessage = wasAmend ? "Commit amended." : "Commit created.";
            await RefreshAsync();
        }
        else
        {
            StatusMessage = result.Error;
        }
    }

    private async Task DiscardFileAsync(StatusEntry? entry)
    {
        if (entry == null) return;
        var result = await _client.Commit.DiscardAsync(_repoPath, entry.Path);
        StatusMessage = result.Success ? $"Discarded: {entry.Path}" : result.Error;
        await RefreshAsync();
    }

    private bool CanCommit() =>
        (AmendMode) || (!string.IsNullOrWhiteSpace(CommitMessage) && StagedFiles.Count > 0);
}
