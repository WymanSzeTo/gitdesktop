namespace GitDesktop.Core.Models;

/// <summary>
/// Represents a bisect session state.
/// </summary>
public sealed class BisectState
{
    public bool IsActive { get; init; }
    public string? GoodCommit { get; init; }
    public string? BadCommit { get; init; }
    public string? CurrentCommit { get; init; }
    public int RemainingSteps { get; init; }
    public IReadOnlyList<string> GoodCommits { get; init; } = [];
    public IReadOnlyList<string> BadCommits { get; init; } = [];
}
