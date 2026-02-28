using System.Collections.ObjectModel;
using System.Windows.Input;
using GitDesktop.Core;
using GitDesktop.Core.Models;

namespace GitDesktop.App.ViewModels;

/// <summary>
/// ViewModel for the remotes view. Lists remotes and supports adding,
/// removing, and renaming remotes.
/// </summary>
public sealed class RemotesViewModel : ViewModelBase
{
    private readonly GitDesktopClient _client;
    private readonly string _repoPath;
    private bool _isLoading;
    private Remote? _selectedRemote;
    private string _newRemoteName = string.Empty;
    private string _newRemoteUrl = string.Empty;
    private string? _statusMessage;

    public RemotesViewModel(GitDesktopClient client, string repoPath)
    {
        _client = client;
        _repoPath = repoPath;

        Remotes = [];

        AddRemoteCommand = new AsyncRelayCommand(AddRemoteAsync, CanAddRemote);
        RemoveRemoteCommand = new AsyncRelayCommand<Remote>(RemoveRemoteAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    /// <summary>Gets or sets a value indicating whether the view is loading data.</summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    /// <summary>Gets or sets the currently selected remote.</summary>
    public Remote? SelectedRemote
    {
        get => _selectedRemote;
        set => SetField(ref _selectedRemote, value);
    }

    /// <summary>Gets or sets the name for the new remote.</summary>
    public string NewRemoteName
    {
        get => _newRemoteName;
        set
        {
            SetField(ref _newRemoteName, value);
            ((AsyncRelayCommand)AddRemoteCommand).RaiseCanExecuteChanged();
        }
    }

    /// <summary>Gets or sets the URL for the new remote.</summary>
    public string NewRemoteUrl
    {
        get => _newRemoteUrl;
        set
        {
            SetField(ref _newRemoteUrl, value);
            ((AsyncRelayCommand)AddRemoteCommand).RaiseCanExecuteChanged();
        }
    }

    /// <summary>Gets or sets a transient status message shown to the user.</summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    /// <summary>Gets the list of all remotes.</summary>
    public ObservableCollection<Remote> Remotes { get; }

    /// <summary>Command to add a new remote.</summary>
    public ICommand AddRemoteCommand { get; }

    /// <summary>Command to remove a remote.</summary>
    public ICommand RemoveRemoteCommand { get; }

    /// <summary>Command to refresh the remotes list.</summary>
    public ICommand RefreshCommand { get; }

    /// <summary>Loads the remotes list asynchronously.</summary>
    public async Task RefreshAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var remotes = await _client.Remote.ListRemotesAsync(_repoPath);
            Remotes.Clear();
            foreach (var r in remotes) Remotes.Add(r);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AddRemoteAsync()
    {
        if (string.IsNullOrWhiteSpace(NewRemoteName) || string.IsNullOrWhiteSpace(NewRemoteUrl)) return;
        var result = await _client.Remote.AddAsync(_repoPath, NewRemoteName, NewRemoteUrl);
        if (result.Success)
        {
            StatusMessage = $"Remote '{NewRemoteName}' added.";
            NewRemoteName = string.Empty;
            NewRemoteUrl = string.Empty;
            await RefreshAsync();
        }
        else
        {
            StatusMessage = result.Error;
        }
    }

    private async Task RemoveRemoteAsync(Remote? remote)
    {
        if (remote == null) return;
        var result = await _client.Remote.RemoveAsync(_repoPath, remote.Name);
        StatusMessage = result.Success ? $"Remote '{remote.Name}' removed." : result.Error;
        await RefreshAsync();
    }

    private bool CanAddRemote() => !string.IsNullOrWhiteSpace(NewRemoteName) && !string.IsNullOrWhiteSpace(NewRemoteUrl);
}
