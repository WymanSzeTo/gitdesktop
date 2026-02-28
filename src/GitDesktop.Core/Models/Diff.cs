namespace GitDesktop.Core.Models;

public enum DiffLineType { Context, Added, Removed, Header, Hunk }

/// <summary>
/// Represents a single line in a diff.
/// </summary>
public sealed class DiffLine
{
    public DiffLineType Type { get; init; }
    public string Content { get; init; } = string.Empty;
    public int? OldLineNumber { get; init; }
    public int? NewLineNumber { get; init; }
}

/// <summary>
/// Represents a hunk within a file diff.
/// </summary>
public sealed class DiffHunk
{
    public string Header { get; init; } = string.Empty;
    public int OldStart { get; init; }
    public int OldCount { get; init; }
    public int NewStart { get; init; }
    public int NewCount { get; init; }
    public IReadOnlyList<DiffLine> Lines { get; init; } = [];
}

/// <summary>
/// Represents the diff of a single file.
/// </summary>
public sealed class FileDiff
{
    public string OldPath { get; init; } = string.Empty;
    public string NewPath { get; init; } = string.Empty;
    public FileStatusKind Status { get; init; }
    public bool IsBinary { get; init; }
    public IReadOnlyList<DiffHunk> Hunks { get; init; } = [];

    public string DisplayPath => string.IsNullOrEmpty(NewPath) ? OldPath : NewPath;
}
