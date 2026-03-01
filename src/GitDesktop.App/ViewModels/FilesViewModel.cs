using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using GitDesktop.Core;

namespace GitDesktop.App.ViewModels;

/// <summary>
/// ViewModel that lists all tracked files in the repository using
/// <c>git ls-files</c> and displays the content of a selected file
/// with basic syntax highlighting.
/// </summary>
public sealed class FilesViewModel : ViewModelBase
{
    private readonly GitDesktopClient _client;
    private readonly string _repoPath;
    private bool _isLoading;
    private bool _isContentLoading;
    private string _filterText = string.Empty;
    private string? _selectedFile;

    public FilesViewModel(GitDesktopClient client, string repoPath)
    {
        _client   = client;
        _repoPath = repoPath;

        AllFiles      = [];
        FilteredFiles = [];
        ContentLines  = [];

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    /// <summary>Gets or sets whether the file list is currently loading.</summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetField(ref _isLoading, value);
    }

    /// <summary>Gets or sets whether the file content panel is currently loading.</summary>
    public bool IsContentLoading
    {
        get => _isContentLoading;
        private set => SetField(ref _isContentLoading, value);
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

    /// <summary>Gets or sets the path of the file currently selected for content viewing.</summary>
    public string? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if (SetField(ref _selectedFile, value))
                _ = LoadFileContentAsync(value);
        }
    }

    /// <summary>Gets all tracked files (unfiltered).</summary>
    public ObservableCollection<string> AllFiles { get; }

    /// <summary>Gets the filtered file list, bound to the list view.</summary>
    public ObservableCollection<string> FilteredFiles { get; }

    /// <summary>Gets the syntax-highlighted lines of the currently selected file.</summary>
    public ObservableCollection<FileLineViewModel> ContentLines { get; }

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

    /// <summary>
    /// Loads the content of <paramref name="filePath"/> and populates <see cref="ContentLines"/>.
    /// First attempts <c>git show HEAD:&lt;path&gt;</c> to get the committed version.
    /// If that fails (e.g. the file is newly staged but not yet committed), falls back to
    /// reading the file directly from the working tree so the content is always visible.
    /// Exposed as <see langword="public"/> so callers (e.g. tests) can await it directly.
    /// </summary>
    public async Task LoadFileContentAsync(string? filePath)
    {
        ContentLines.Clear();
        if (string.IsNullOrWhiteSpace(filePath)) return;

        IsContentLoading = true;
        try
        {
            // Quote the path to handle spaces and special characters safely.
            var safePath = filePath.Replace("\"", "\\\"");
            var result = await _client.Advanced.RunRawAsync(_repoPath, $"show HEAD:\"{safePath}\"");

            string? content = null;
            if (result.Success && !string.IsNullOrEmpty(result.Output))
            {
                content = result.Output;
            }
            else
            {
                // Fallback: read from the working tree so newly staged / modified files are shown.
                var fullPath = Path.GetFullPath(filePath, _repoPath);
                if (File.Exists(fullPath))
                    content = await File.ReadAllTextAsync(fullPath);
            }

            if (content == null) return;

            foreach (var line in content.Split('\n'))
                ContentLines.Add(new FileLineViewModel(line, ClassifyLine(line)));
        }
        finally
        {
            IsContentLoading = false;
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

    // ── Basic line-level syntax classification ────────────────────────────────

    private static readonly string[] s_keywords =
    [
        "public", "private", "protected", "internal", "static", "readonly", "const",
        "class", "interface", "struct", "enum", "namespace", "using", "void", "return",
        "if", "else", "for", "foreach", "while", "do", "switch", "case", "break",
        "continue", "new", "null", "true", "false", "var", "let", "const", "function",
        "def", "import", "from", "async", "await", "try", "catch", "finally", "throw",
    ];

    /// <summary>Classifies a single source line into a <see cref="FileLineKind"/>.</summary>
    public static FileLineKind ClassifyLine(string line)
    {
        var trimmed = line.TrimStart();

        // Comment patterns (C#, Java, JS, Python, Ruby, Shell, SQL, CSS)
        if (trimmed.StartsWith("//") || trimmed.StartsWith("#") ||
            trimmed.StartsWith("*")  || trimmed.StartsWith("/*") ||
            trimmed.StartsWith("--") || trimmed.StartsWith("<!--"))
            return FileLineKind.Comment;

        // String literals: line is predominantly a string assignment
        if ((trimmed.Contains('"') || trimmed.Contains('\'')) &&
            (trimmed.StartsWith("\"") || trimmed.StartsWith("'") ||
             trimmed.Contains(" = \"") || trimmed.Contains(" = '")))
            return FileLineKind.String;

        // Keyword lines: starts with a known keyword
        foreach (var kw in s_keywords)
        {
            if (trimmed.StartsWith(kw + " ") || trimmed.StartsWith(kw + "\t") ||
                trimmed == kw)
                return FileLineKind.Keyword;
        }

        return FileLineKind.Code;
    }
}

