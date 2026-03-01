namespace GitDesktop.App.ViewModels;

/// <summary>
/// Represents a single line in a source-file view with basic syntax-highlighting semantics.
/// The colour is determined by heuristic line-level analysis (comments, keywords, etc.).
/// </summary>
public sealed class FileLineViewModel
{
    public FileLineViewModel(string content, FileLineKind kind = FileLineKind.Code)
    {
        Content = content;
        Kind    = kind;
    }

    public string      Content { get; }
    public FileLineKind Kind   { get; }

    /// <summary>Foreground brush resource key for this line.</summary>
    public string ForegroundKey => Kind switch
    {
        FileLineKind.Comment => "ThemeSecondaryText",
        FileLineKind.Keyword => "ThemeAccentColor",
        FileLineKind.String  => "ThemeAddedForeground",
        _                    => "ThemePrimaryText",
    };
}

/// <summary>Basic syntax category used for line-level colouring in the file viewer.</summary>
public enum FileLineKind
{
    Code,
    Comment,
    Keyword,
    String,
}
