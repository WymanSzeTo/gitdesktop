namespace GitDesktop.Core.Models;

public enum BranchType { Local, Remote }

/// <summary>
/// Represents a Git branch.
/// </summary>
public sealed class Branch
{
    public string Name { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public BranchType Type { get; init; }
    public bool IsCurrentBranch { get; init; }
    public string? UpstreamName { get; init; }
    public int AheadCount { get; init; }
    public int BehindCount { get; init; }
    public string TipHash { get; init; } = string.Empty;

    public bool IsLocal => Type == BranchType.Local;
    public bool IsRemote => Type == BranchType.Remote;
    public bool HasUpstream => UpstreamName != null;

    public override string ToString() => Name;
}
