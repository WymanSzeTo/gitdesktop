using GitDesktop.Core.Models;

namespace GitDesktop.App.ViewModels;

/// <summary>
/// Represents a single line in a diff, along with the colour semantics
/// required by the diff view.
/// </summary>
public sealed class DiffLineViewModel
{
    public DiffLineViewModel(DiffLine line)
    {
        Content = line.Content;
        Type    = line.Type;
    }

    /// <summary>Creates a header-only line (e.g. "--- a/file", "+++ b/file").</summary>
    public DiffLineViewModel(string content, DiffLineType type = DiffLineType.Header)
    {
        Content = content;
        Type    = type;
    }

    public string      Content { get; }
    public DiffLineType Type   { get; }

    /// <summary>Background brush key to look up via DynamicResource.</summary>
    public string BackgroundKey => Type switch
    {
        DiffLineType.Added   => "ThemeAddedBackground",
        DiffLineType.Removed => "ThemeRemovedBackground",
        DiffLineType.Hunk    => "ThemeHunkBackground",
        _                    => "ThemeContentBackground",
    };

    /// <summary>Foreground brush key to look up via DynamicResource.</summary>
    public string ForegroundKey => Type switch
    {
        DiffLineType.Added   => "ThemeAddedForeground",
        DiffLineType.Removed => "ThemeRemovedForeground",
        DiffLineType.Hunk    => "ThemeHunkForeground",
        DiffLineType.Header  => "ThemeSecondaryText",
        _                    => "ThemeDiffContextForeground",
    };
}
