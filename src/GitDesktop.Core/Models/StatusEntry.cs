namespace GitDesktop.Core.Models;

public enum FileStatusKind
{
    Untracked,
    Modified,
    Added,
    Deleted,
    Renamed,
    Copied,
    Unmerged,
    Ignored,
}

/// <summary>
/// Represents a file entry in the working tree / index status.
/// </summary>
public sealed class StatusEntry
{
    public string Path { get; init; } = string.Empty;
    public string? OriginalPath { get; init; }
    public FileStatusKind IndexStatus { get; init; }
    public FileStatusKind WorkTreeStatus { get; init; }
    public bool IsStaged => IndexStatus != FileStatusKind.Untracked && IndexStatus != FileStatusKind.Modified;
    public bool IsConflicted => IndexStatus == FileStatusKind.Unmerged || WorkTreeStatus == FileStatusKind.Unmerged;

    public override string ToString() => Path;
}

/// <summary>
/// Overall repository status.
/// </summary>
public sealed class RepositoryStatus
{
    public string CurrentBranch { get; init; } = string.Empty;
    public string? UpstreamBranch { get; init; }
    public int AheadCount { get; init; }
    public int BehindCount { get; init; }
    public bool IsDetachedHead { get; init; }
    public IReadOnlyList<StatusEntry> Entries { get; init; } = [];

    public IEnumerable<StatusEntry> StagedEntries => Entries.Where(e => e.IndexStatus != FileStatusKind.Untracked && e.IndexStatus != FileStatusKind.Modified);
    public IEnumerable<StatusEntry> UnstagedEntries => Entries.Where(e => e.WorkTreeStatus == FileStatusKind.Modified || e.WorkTreeStatus == FileStatusKind.Deleted);
    public IEnumerable<StatusEntry> UntrackedEntries => Entries.Where(e => e.WorkTreeStatus == FileStatusKind.Untracked);
    public IEnumerable<StatusEntry> ConflictedEntries => Entries.Where(e => e.IsConflicted);
}
