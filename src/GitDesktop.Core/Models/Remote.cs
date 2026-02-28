namespace GitDesktop.Core.Models;

/// <summary>
/// Represents a Git remote.
/// </summary>
public sealed class Remote
{
    public string Name { get; init; } = string.Empty;
    public string FetchUrl { get; init; } = string.Empty;
    public string? PushUrl { get; init; }
    public IReadOnlyList<string> FetchRefSpecs { get; init; } = [];
    public IReadOnlyList<string> PushRefSpecs { get; init; } = [];

    public override string ToString() => $"{Name} {FetchUrl}";
}
