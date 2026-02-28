namespace GitDesktop.Core.Models;

/// <summary>
/// Represents a blame annotation for a line in a file.
/// </summary>
public sealed class BlameLine
{
    public int LineNumber { get; init; }
    public string Content { get; init; } = string.Empty;
    public string CommitHash { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public string AuthorEmail { get; init; } = string.Empty;
    public DateTimeOffset AuthorDate { get; init; }
    public string Summary { get; init; } = string.Empty;
}

/// <summary>
/// Represents the blame output for an entire file.
/// </summary>
public sealed class BlameResult
{
    public string FilePath { get; init; } = string.Empty;
    public string Commit { get; init; } = string.Empty;
    public IReadOnlyList<BlameLine> Lines { get; init; } = [];
}
