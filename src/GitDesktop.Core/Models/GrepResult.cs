namespace GitDesktop.Core.Models;

/// <summary>
/// Represents a grep result match.
/// </summary>
public sealed class GrepMatch
{
    public string FilePath { get; init; } = string.Empty;
    public int LineNumber { get; init; }
    public string LineContent { get; init; } = string.Empty;
    public string Ref { get; init; } = string.Empty;
}

/// <summary>
/// Aggregated grep results grouped by file.
/// </summary>
public sealed class GrepResult
{
    public string Pattern { get; init; } = string.Empty;
    public IReadOnlyList<GrepMatch> Matches { get; init; } = [];
}
