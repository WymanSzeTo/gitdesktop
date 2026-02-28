using System.Collections.ObjectModel;
using System.Windows.Input;
using GitDesktop.Core;
using GitDesktop.Core.Models;

namespace GitDesktop.App.ViewModels;

/// <summary>
/// ViewModel for the tags view. Lists tags and supports creating and deleting tags.
/// </summary>
public sealed class TagsViewModel : ViewModelBase
{
    private readonly GitDesktopClient _client;
    private readonly string _repoPath;
    private bool _isLoading;
    private Tag? _selectedTag;
    private string _newTagName = string.Empty;
    private string _newTagMessage = string.Empty;
    private string? _statusMessage;

    public TagsViewModel(GitDesktopClient client, string repoPath)
    {
        _client = client;
        _repoPath = repoPath;

        Tags = [];

        CreateLightweightTagCommand = new AsyncRelayCommand(CreateLightweightTagAsync, CanCreateTag);
        CreateAnnotatedTagCommand = new AsyncRelayCommand(CreateAnnotatedTagAsync, CanCreateAnnotatedTag);
        DeleteTagCommand = new AsyncRelayCommand<Tag>(DeleteTagAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    /// <summary>Gets or sets a value indicating whether the view is loading data.</summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    /// <summary>Gets or sets the currently selected tag.</summary>
    public Tag? SelectedTag
    {
        get => _selectedTag;
        set => SetField(ref _selectedTag, value);
    }

    /// <summary>Gets or sets the name for the new tag to create.</summary>
    public string NewTagName
    {
        get => _newTagName;
        set
        {
            SetField(ref _newTagName, value);
            ((AsyncRelayCommand)CreateLightweightTagCommand).RaiseCanExecuteChanged();
            ((AsyncRelayCommand)CreateAnnotatedTagCommand).RaiseCanExecuteChanged();
        }
    }

    /// <summary>Gets or sets the message for the new annotated tag.</summary>
    public string NewTagMessage
    {
        get => _newTagMessage;
        set
        {
            SetField(ref _newTagMessage, value);
            ((AsyncRelayCommand)CreateAnnotatedTagCommand).RaiseCanExecuteChanged();
        }
    }

    /// <summary>Gets or sets a transient status message shown to the user.</summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    /// <summary>Gets the list of all tags.</summary>
    public ObservableCollection<Tag> Tags { get; }

    /// <summary>Command to create a lightweight tag.</summary>
    public ICommand CreateLightweightTagCommand { get; }

    /// <summary>Command to create an annotated tag.</summary>
    public ICommand CreateAnnotatedTagCommand { get; }

    /// <summary>Command to delete a tag.</summary>
    public ICommand DeleteTagCommand { get; }

    /// <summary>Command to refresh the tags list.</summary>
    public ICommand RefreshCommand { get; }

    /// <summary>Loads the tags list asynchronously.</summary>
    public async Task RefreshAsync()
    {
        IsLoading = true;
        StatusMessage = null;
        try
        {
            var tags = await _client.Branch.ListTagsAsync(_repoPath);
            Tags.Clear();
            foreach (var t in tags) Tags.Add(t);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CreateLightweightTagAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTagName)) return;
        var result = await _client.Branch.CreateLightweightTagAsync(_repoPath, NewTagName);
        if (result.Success)
        {
            StatusMessage = $"Tag '{NewTagName}' created.";
            NewTagName = string.Empty;
            await RefreshAsync();
        }
        else
        {
            StatusMessage = result.Error;
        }
    }

    private async Task CreateAnnotatedTagAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTagName) || string.IsNullOrWhiteSpace(NewTagMessage)) return;
        var result = await _client.Branch.CreateAnnotatedTagAsync(_repoPath, NewTagName, NewTagMessage);
        if (result.Success)
        {
            StatusMessage = $"Annotated tag '{NewTagName}' created.";
            NewTagName = string.Empty;
            NewTagMessage = string.Empty;
            await RefreshAsync();
        }
        else
        {
            StatusMessage = result.Error;
        }
    }

    private async Task DeleteTagAsync(Tag? tag)
    {
        if (tag == null) return;
        var result = await _client.Branch.DeleteTagAsync(_repoPath, tag.Name);
        StatusMessage = result.Success ? $"Tag '{tag.Name}' deleted." : result.Error;
        await RefreshAsync();
    }

    private bool CanCreateTag() => !string.IsNullOrWhiteSpace(NewTagName);

    private bool CanCreateAnnotatedTag() => !string.IsNullOrWhiteSpace(NewTagName) && !string.IsNullOrWhiteSpace(NewTagMessage);
}
