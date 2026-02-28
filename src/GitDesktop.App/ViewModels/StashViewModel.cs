using System.Collections.ObjectModel;
using System.Windows.Input;
using GitDesktop.Core;
using GitDesktop.Core.Models;

namespace GitDesktop.App.ViewModels;

/// <summary>
/// ViewModel for the stash view. Lists stashes and supports apply, pop, and drop operations.
/// </summary>
public sealed class StashViewModel : ViewModelBase
{
    private readonly GitDesktopClient _client;
    private readonly string _repoPath;
    private bool _isLoading;
    private Stash? _selectedStash;
    private string? _selectedStashDiff;
    private string _stashMessage = string.Empty;
    private string? _statusMessage;

    public StashViewModel(GitDesktopClient client, string repoPath)
    {
        _client = client;
        _repoPath = repoPath;

        Stashes = [];

        PushStashCommand = new AsyncRelayCommand(PushStashAsync);
        ApplyStashCommand = new AsyncRelayCommand<Stash>(ApplyStashAsync);
        PopStashCommand = new AsyncRelayCommand<Stash>(PopStashAsync);
        DropStashCommand = new AsyncRelayCommand<Stash>(DropStashAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    /// <summary>Gets or sets a value indicating whether the view is loading data.</summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    /// <summary>Gets or sets the currently selected stash.</summary>
    public Stash? SelectedStash
    {
        get => _selectedStash;
        set
        {
            if (SetField(ref _selectedStash, value))
            {
                SelectedStashDiff = null;
                if (value != null)
                    _ = LoadStashDiffAsync(value);
            }
        }
    }

    /// <summary>Gets or sets the diff text for the selected stash.</summary>
    public string? SelectedStashDiff
    {
        get => _selectedStashDiff;
        private set => SetField(ref _selectedStashDiff, value);
    }

    /// <summary>Gets or sets an optional message for a new stash.</summary>
    public string StashMessage
    {
        get => _stashMessage;
        set => SetField(ref _stashMessage, value);
    }

    /// <summary>Gets or sets a transient status message shown to the user.</summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    /// <summary>Gets the list of all stashes.</summary>
    public ObservableCollection<Stash> Stashes { get; }

    /// <summary>Command to push a new stash.</summary>
    public ICommand PushStashCommand { get; }

    /// <summary>Command to apply a stash (keep it in the list).</summary>
    public ICommand ApplyStashCommand { get; }

    /// <summary>Command to pop a stash (apply and remove).</summary>
    public ICommand PopStashCommand { get; }

    /// <summary>Command to drop a stash.</summary>
    public ICommand DropStashCommand { get; }

    /// <summary>Command to refresh the stash list.</summary>
    public ICommand RefreshCommand { get; }

    /// <summary>Loads the stash list asynchronously.</summary>
    public async Task RefreshAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var stashes = await _client.Commit.StashListAsync(_repoPath);
            Stashes.Clear();
            foreach (var s in stashes) Stashes.Add(s);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task PushStashAsync()
    {
        var message = string.IsNullOrWhiteSpace(StashMessage) ? null : StashMessage;
        var result = await _client.Commit.StashPushAsync(_repoPath, message);
        if (result.Success)
        {
            StatusMessage = "Changes stashed.";
            StashMessage = string.Empty;
            await RefreshAsync();
        }
        else
        {
            StatusMessage = result.Error;
        }
    }

    private async Task ApplyStashAsync(Stash? stash)
    {
        if (stash == null) return;
        var result = await _client.Commit.StashApplyAsync(_repoPath, stash.Index);
        StatusMessage = result.Success ? $"Applied stash@{{{stash.Index}}}." : result.Error;
    }

    private async Task PopStashAsync(Stash? stash)
    {
        if (stash == null) return;
        var result = await _client.Commit.StashPopAsync(_repoPath, stash.Index);
        StatusMessage = result.Success ? $"Popped stash@{{{stash.Index}}}." : result.Error;
        if (result.Success) await RefreshAsync();
    }

    private async Task DropStashAsync(Stash? stash)
    {
        if (stash == null) return;
        var result = await _client.Commit.StashDropAsync(_repoPath, stash.Index);
        StatusMessage = result.Success ? $"Dropped stash@{{{stash.Index}}}." : result.Error;
        if (result.Success) await RefreshAsync();
    }

    private async Task LoadStashDiffAsync(Stash stash)
    {
        var result = await _client.Commit.StashShowAsync(_repoPath, stash.Index);
        SelectedStashDiff = result.Success ? result.Output : result.Error;
    }
}
