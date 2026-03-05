using GitDesktop.Core.Models;

namespace GitDesktop.App.ViewModels;

public sealed class HistoryDiffLineViewModel
{
    public HistoryDiffLineViewModel(string content, DiffLineType type, FileLineKind syntaxKind)
    {
        Content = content;
        Type = type;
        SyntaxKind = syntaxKind;
    }

    public string Content { get; }
    public DiffLineType Type { get; }
    public FileLineKind SyntaxKind { get; }

    public string BackgroundKey => Type switch
    {
        DiffLineType.Added => "ThemeAddedBackground",
        DiffLineType.Removed => "ThemeRemovedBackground",
        DiffLineType.Hunk => "ThemeHunkBackground",
        _ => "ThemeContentBackground",
    };

    public string ForegroundKey => Type switch
    {
        DiffLineType.Added => "ThemeAddedForeground",
        DiffLineType.Removed => "ThemeRemovedForeground",
        DiffLineType.Hunk => "ThemeHunkForeground",
        DiffLineType.Header => "ThemeSecondaryText",
        _ => SyntaxKind switch
        {
            FileLineKind.Comment => "ThemeSyntaxComment",
            FileLineKind.Keyword => "ThemeSyntaxKeyword",
            FileLineKind.String => "ThemeSyntaxString",
            _ => "ThemeDiffContextForeground",
        },
    };
}
