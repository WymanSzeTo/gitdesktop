namespace GitDesktop.Core.Models;

/// <summary>
/// Represents a Git repository.
/// </summary>
public sealed class Repository
{
    public string Path { get; init; } = string.Empty;
    public string GitDirectory { get; init; } = string.Empty;
    public bool IsBare { get; init; }
    public string DefaultBranch { get; init; } = "main";
    public string GitVersion { get; init; } = string.Empty;

    public override string ToString() => Path;
}
