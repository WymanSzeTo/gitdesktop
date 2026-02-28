using System.Collections.ObjectModel;
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
    private readonly GitDesktopClient _client;
    private readonly string _repoPath;
    private bool _isLoading;
    private Commit? _selectedCommit;
    private string? _selectedCommitDiff;

    public HistoryViewModel(GitDesktopClient client, string repoPath)
    {
        _client = client;
        _repoPath = repoPath;

        Commits = [];

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        LoadDiffCommand = new AsyncRelayCommand<Commit>(LoadDiffAsync);
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

    /// <summary>Gets the list of commits in the log.</summary>
    public ObservableCollection<Commit> Commits { get; }

    /// <summary>Command to refresh the commit log.</summary>
    public ICommand RefreshCommand { get; }

    /// <summary>Command to load the diff for a specific commit.</summary>
    public ICommand LoadDiffCommand { get; }

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
    }
}
