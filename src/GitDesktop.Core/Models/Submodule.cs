namespace GitDesktop.Core.Models;

public enum SubmoduleStatus
{
    Uninitialized,
    UpToDate,
    Ahead,
    Behind,
    Modified,
    Conflict,
}

/// <summary>
/// Represents a git submodule.
/// </summary>
public sealed class Submodule
{
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string? Branch { get; init; }
    public string CommitHash { get; init; } = string.Empty;
    public SubmoduleStatus Status { get; init; }

    public override string ToString() => $"{Name} ({Path})";
}
