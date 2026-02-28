namespace GitDesktop.Core.Models;

public enum WorkTreeStatus { Clean, Dirty, Locked, Prunable }

/// <summary>
/// Represents a git worktree.
/// </summary>
public sealed record WorkTree
{
    public string Path { get; init; } = string.Empty;
    public string? Branch { get; init; }
    public string HeadHash { get; init; } = string.Empty;
    public bool IsMainWorktree { get; init; }
    public bool IsBare { get; init; }
    public bool IsDetached { get; init; }
    public WorkTreeStatus Status { get; init; }
    public string? LockReason { get; init; }

    public override string ToString() => Path;
}
