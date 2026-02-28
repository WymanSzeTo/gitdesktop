using System.Collections.ObjectModel;
using System.Windows.Input;
using GitDesktop.Core;
using GitDesktop.Core.Models;

namespace GitDesktop.App.ViewModels;

/// <summary>
/// ViewModel for the branches view. Lists local and remote branches and
/// supports creating, switching, and deleting branches.
/// </summary>
public sealed class BranchesViewModel : ViewModelBase
{
    private readonly GitDesktopClient _client;
    private readonly string _repoPath;
    private bool _isLoading;
    private Branch? _selectedBranch;
    private string _newBranchName = string.Empty;
    private string? _statusMessage;

    public BranchesViewModel(GitDesktopClient client, string repoPath)
    {
        _client = client;
        _repoPath = repoPath;

        Branches = [];

        SwitchBranchCommand = new AsyncRelayCommand<Branch>(SwitchBranchAsync);
        CreateBranchCommand = new AsyncRelayCommand(CreateBranchAsync, CanCreateBranch);
        DeleteBranchCommand = new AsyncRelayCommand<Branch>(DeleteBranchAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    /// <summary>Gets or sets a value indicating whether the view is loading data.</summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    /// <summary>Gets or sets the currently selected branch.</summary>
    public Branch? SelectedBranch
    {
        get => _selectedBranch;
        set => SetField(ref _selectedBranch, value);
    }

    /// <summary>Gets or sets the name for the new branch to create.</summary>
    public string NewBranchName
    {
        get => _newBranchName;
        set
        {
            SetField(ref _newBranchName, value);
            ((AsyncRelayCommand)CreateBranchCommand).RaiseCanExecuteChanged();
        }
    }

    /// <summary>Gets or sets a transient status message shown to the user.</summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    /// <summary>Gets the list of all branches (local and remote).</summary>
    public ObservableCollection<Branch> Branches { get; }

    /// <summary>Command to switch to a branch.</summary>
    public ICommand SwitchBranchCommand { get; }

    /// <summary>Command to create a new branch from the current HEAD.</summary>
    public ICommand CreateBranchCommand { get; }

    /// <summary>Command to delete a branch.</summary>
    public ICommand DeleteBranchCommand { get; }

    /// <summary>Command to refresh the branches list.</summary>
    public ICommand RefreshCommand { get; }

    /// <summary>Loads the branches list asynchronously.</summary>
    public async Task RefreshAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var branches = await _client.Branch.ListBranchesAsync(_repoPath, includeRemotes: true);
            Branches.Clear();
            foreach (var b in branches) Branches.Add(b);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SwitchBranchAsync(Branch? branch)
    {
        if (branch == null || !branch.IsLocal) return;
        var result = await _client.Branch.SwitchAsync(_repoPath, branch.Name);
        StatusMessage = result.Success ? $"Switched to branch '{branch.Name}'." : result.Error;
        await RefreshAsync();
    }

    private async Task CreateBranchAsync()
    {
        if (string.IsNullOrWhiteSpace(NewBranchName)) return;
        var result = await _client.Branch.CreateAsync(_repoPath, NewBranchName);
        if (result.Success)
        {
            StatusMessage = $"Branch '{NewBranchName}' created.";
            NewBranchName = string.Empty;
            await RefreshAsync();
        }
        else
        {
            StatusMessage = result.Error;
        }
    }

    private async Task DeleteBranchAsync(Branch? branch)
    {
        if (branch == null || !branch.IsLocal || branch.IsCurrentBranch) return;
        var result = await _client.Branch.DeleteAsync(_repoPath, branch.Name);
        StatusMessage = result.Success ? $"Branch '{branch.Name}' deleted." : result.Error;
        await RefreshAsync();
    }

    private bool CanCreateBranch() => !string.IsNullOrWhiteSpace(NewBranchName);
}
