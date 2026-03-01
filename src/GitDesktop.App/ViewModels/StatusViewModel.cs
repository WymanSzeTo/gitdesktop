using System.Collections.ObjectModel;
using System.Windows.Input;
using GitDesktop.Core;
using GitDesktop.Core.Models;

namespace GitDesktop.App.ViewModels;

/// <summary>
/// ViewModel for the repository status view. Shows staged, unstaged, and
/// untracked files, supports staging/unstaging individual files, and shows
/// a syntax-highlighted diff for the selected file.
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
    private StatusEntry? _selectedFile;
    private bool _isDiffLoading;

    public StatusViewModel(GitDesktopClient client, string repoPath)
    {
        _client = client;
        _repoPath = repoPath;

        StagedFiles   = [];
        UnstagedFiles = [];
        UntrackedFiles = [];
        DiffLines      = [];

        StageFileCommand   = new AsyncRelayCommand<StatusEntry>(StageFileAsync);
        UnstageFileCommand = new AsyncRelayCommand<StatusEntry>(UnstageFileAsync);
        StageAllCommand    = new AsyncRelayCommand(StageAllAsync);
        CommitCommand      = new AsyncRelayCommand(CommitAsync, CanCommit);
        DiscardFileCommand = new AsyncRelayCommand<StatusEntry>(DiscardFileAsync);
        RefreshCommand     = new AsyncRelayCommand(RefreshAsync);
    }

    // ── Diff panel ────────────────────────────────────────────────────────────

    /// <summary>Gets or sets the file whose diff is currently displayed.</summary>
    public StatusEntry? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if (SetField(ref _selectedFile, value))
                _ = LoadDiffAsync(value);
        }
    }

    /// <summary>Gets a value indicating whether the diff panel is loading.</summary>
    public bool IsDiffLoading
    {
        get => _isDiffLoading;
        private set => SetField(ref _isDiffLoading, value);
    }

    /// <summary>Gets the diff lines for the selected file.</summary>
    public ObservableCollection<DiffLineViewModel> DiffLines { get; }

    /// <summary>
    /// <summary>
    /// Loads the diff for <paramref name="entry"/> into <see cref="DiffLines"/>.
    /// Exposed as <see langword="public"/> so callers (e.g. tests) can await it directly.
    /// The <see cref="SelectedFile"/> setter calls this as fire-and-forget.
    /// </summary>
    public async Task LoadDiffAsync(StatusEntry? entry)
    {
        DiffLines.Clear();
        if (entry == null) return;

        IsDiffLoading = true;
        try
        {
            var result = await _client.History.DiffAsync(_repoPath, entry.Path,
                cached: ShouldUseCachedDiff(entry));
            if (!result.Success || string.IsNullOrWhiteSpace(result.Output))
                return;

            foreach (var vm in ParseDiffLines(result.Output))
                DiffLines.Add(vm);
        }
        finally
        {
            IsDiffLoading = false;
        }
    }
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

    // ── Diff loading ──────────────────────────────────────────────────────────

    private static bool ShouldUseCachedDiff(StatusEntry entry)
    {
        // Use --cached (diff index vs HEAD) for files that are staged.
        // Staged files have an IndexStatus other than Untracked or Modified
        // (i.e. Added, Renamed, Copied, Deleted, or Unmerged).
        // For working-tree changes (Modified, Untracked) use the plain diff.
        return entry.IndexStatus != FileStatusKind.Untracked
            && entry.IndexStatus != FileStatusKind.Modified;
    }

    /// <summary>Converts raw unified-diff text into a list of <see cref="DiffLineViewModel"/>.</summary>
    public static IEnumerable<DiffLineViewModel> ParseDiffLines(string diffText)
    {
        foreach (var rawLine in diffText.Split('\n'))
        {
            var lineType = rawLine.Length == 0
                ? DiffLineType.Context
                : rawLine[0] switch
                {
                    '+' when rawLine.StartsWith("+++") => DiffLineType.Header,
                    '-' when rawLine.StartsWith("---") => DiffLineType.Header,
                    '+' => DiffLineType.Added,
                    '-' => DiffLineType.Removed,
                    '@' => DiffLineType.Hunk,
                    'd' when rawLine.StartsWith("diff ") => DiffLineType.Header,
                    _ => DiffLineType.Context,
                };
            yield return new DiffLineViewModel(new DiffLine { Content = rawLine, Type = lineType });
        }
    }
}
