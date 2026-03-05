using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using GitDesktop.Core;
using GitDesktop.Core.Models;

namespace GitDesktop.App.ViewModels;

/// <summary>
/// ViewModel for the commit history view. Shows the commit log and
/// displays details for the selected commit.
/// </summary>
public sealed class HistoryViewModel : ViewModelBase
{
    private const string UnknownLanguageLabel = "Unknown";
    private const string MixedLanguageLabel = "Mixed";

    private readonly GitDesktopClient _client;
    private readonly string _repoPath;
    private bool _isLoading;
    private Commit? _selectedCommit;
    private string? _selectedCommitDiff;
    private string? _statusMessage;
    private string _detectedDiffLanguage = UnknownLanguageLabel;
    private static readonly Regex s_diffFileRegex = new(@"^\+\+\+ b/(.+)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public HistoryViewModel(GitDesktopClient client, string repoPath)
    {
        _client = client;
        _repoPath = repoPath;

        Commits = [];
        SelectedCommitDiffLines = [];

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        LoadDiffCommand = new AsyncRelayCommand<Commit>(LoadDiffAsync);
        CherryPickCommand = new AsyncRelayCommand<Commit>(CherryPickAsync);
        RevertCommand = new AsyncRelayCommand<Commit>(RevertAsync);
        ResetToCommitCommand = new AsyncRelayCommand<Commit>(ResetToCommitAsync);
    }

    /// <summary>Gets or sets a value indicating whether the view is loading data.</summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    /// <summary>Gets or sets the currently selected commit.</summary>
    public Commit? SelectedCommit
    {
        get => _selectedCommit;
        set
        {
            if (SetField(ref _selectedCommit, value))
            {
                SelectedCommitDiff = null;
                if (value != null)
                    _ = LoadDiffAsync(value);
            }
        }
    }

    /// <summary>Gets or sets the diff text for the selected commit.</summary>
    public string? SelectedCommitDiff
    {
        get => _selectedCommitDiff;
        private set => SetField(ref _selectedCommitDiff, value);
    }

    public string DetectedDiffLanguage
    {
        get => _detectedDiffLanguage;
        private set => SetField(ref _detectedDiffLanguage, value);
    }

    /// <summary>Gets or sets a transient status message shown to the user.</summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    /// <summary>Gets the list of commits in the log.</summary>
    public ObservableCollection<Commit> Commits { get; }
    public ObservableCollection<HistoryDiffLineViewModel> SelectedCommitDiffLines { get; }

    /// <summary>Command to refresh the commit log.</summary>
    public ICommand RefreshCommand { get; }

    /// <summary>Command to load the diff for a specific commit.</summary>
    public ICommand LoadDiffCommand { get; }

    /// <summary>Command to cherry-pick a commit.</summary>
    public ICommand CherryPickCommand { get; }

    /// <summary>Command to revert a commit.</summary>
    public ICommand RevertCommand { get; }

    /// <summary>Command to reset HEAD to a commit (mixed).</summary>
    public ICommand ResetToCommitCommand { get; }

    /// <summary>Loads the commit history asynchronously.</summary>
    public async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            var commits = await _client.History.GetLogAsync(_repoPath, limit: 100);
            Commits.Clear();
            foreach (var c in commits) Commits.Add(c);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadDiffAsync(Commit? commit)
    {
        if (commit == null) return;
        var result = await _client.History.ShowAsync(_repoPath, commit.Hash);
        SelectedCommitDiff = result.Success ? result.Output : result.Error;
        BuildDiffLines(result.Success ? result.Output : null);
    }

    private void BuildDiffLines(string? diffText)
    {
        SelectedCommitDiffLines.Clear();
        DetectedDiffLanguage = UnknownLanguageLabel;
        if (string.IsNullOrWhiteSpace(diffText))
            return;

        var currentLanguage = SourceLanguage.Unknown;
        var encounteredLanguages = new HashSet<SourceLanguage>();
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

            if (lineType == DiffLineType.Header)
            {
                var match = s_diffFileRegex.Match(rawLine);
                if (match.Success)
                {
                    currentLanguage = SourceSyntaxClassifier.DetectLanguage(match.Groups[1].Value);
                    if (currentLanguage != SourceLanguage.Unknown)
                        encounteredLanguages.Add(currentLanguage);
                }
            }

            var contentForSyntax = lineType is DiffLineType.Added or DiffLineType.Removed
                ? rawLine[1..]
                : rawLine;
            var syntaxKind = lineType is DiffLineType.Context or DiffLineType.Added or DiffLineType.Removed
                ? SourceSyntaxClassifier.ClassifyLine(contentForSyntax, currentLanguage)
                : FileLineKind.Code;

            SelectedCommitDiffLines.Add(new HistoryDiffLineViewModel(rawLine, lineType, syntaxKind));
        }

        DetectedDiffLanguage = encounteredLanguages.Count switch
        {
            0 => UnknownLanguageLabel,
            1 => encounteredLanguages.First().ToString(),
            _ => MixedLanguageLabel,
        };
    }

    private async Task CherryPickAsync(Commit? commit)
    {
        if (commit == null) return;
        var result = await _client.MergeRebase.CherryPickAsync(_repoPath, [commit.Hash]);
        StatusMessage = result.Success ? $"Cherry-picked {commit.ShortHash}." : result.Error;
        if (result.Success) await RefreshAsync();
    }

    private async Task RevertAsync(Commit? commit)
    {
        if (commit == null) return;
        var result = await _client.MergeRebase.RevertAsync(_repoPath, commit.Hash);
        StatusMessage = result.Success ? $"Reverted {commit.ShortHash}." : result.Error;
        if (result.Success) await RefreshAsync();
    }

    private async Task ResetToCommitAsync(Commit? commit)
    {
        if (commit == null) return;
        var result = await _client.MergeRebase.ResetAsync(_repoPath, commit.Hash);
        StatusMessage = result.Success ? $"Reset to {commit.ShortHash}." : result.Error;
        if (result.Success) await RefreshAsync();
    }
}
