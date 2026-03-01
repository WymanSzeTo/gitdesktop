using System.Collections.ObjectModel;
using System.Windows.Input;
using GitDesktop.Core;

namespace GitDesktop.App.ViewModels;

/// <summary>
/// ViewModel that lists all tracked files in the repository using
/// <c>git ls-files</c>.
/// </summary>
public sealed class FilesViewModel : ViewModelBase
{
    private readonly GitDesktopClient _client;
    private readonly string _repoPath;
    private bool _isLoading;
    private string _filterText = string.Empty;

    public FilesViewModel(GitDesktopClient client, string repoPath)
    {
        _client   = client;
        _repoPath = repoPath;

        AllFiles      = [];
        FilteredFiles = [];

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    /// <summary>Gets or sets whether the view is currently loading.</summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    /// <summary>Gets or sets the text filter applied to the file list.</summary>
    public string FilterText
    {
        get => _filterText;
        set
        {
            SetField(ref _filterText, value);
            ApplyFilter();
        }
    }

    /// <summary>Gets all tracked files (unfiltered).</summary>
    public ObservableCollection<string> AllFiles { get; }

    /// <summary>Gets the filtered file list, bound to the list view.</summary>
    public ObservableCollection<string> FilteredFiles { get; }

    /// <summary>Command to reload the file list.</summary>
    public ICommand RefreshCommand { get; }

    /// <summary>Refreshes the file list by running <c>git ls-files</c>.</summary>
    public async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            var result = await _client.Advanced.RunRawAsync(_repoPath, "ls-files");
            AllFiles.Clear();
            if (result.Success)
            {
                foreach (var line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    AllFiles.Add(line.Trim());
            }
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        FilteredFiles.Clear();
        var filter = _filterText.Trim();
        foreach (var f in AllFiles)
        {
            if (string.IsNullOrEmpty(filter) ||
                f.Contains(filter, StringComparison.OrdinalIgnoreCase))
                FilteredFiles.Add(f);
        }
    }
}
