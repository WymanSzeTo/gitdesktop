namespace GitDesktop.Core.Models;

/// <summary>
/// Represents a Git commit.
/// </summary>
public sealed class Commit
{
    public string Hash { get; init; } = string.Empty;
    public string ShortHash { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public string AuthorEmail { get; init; } = string.Empty;
    public DateTimeOffset AuthorDate { get; init; }
    public string CommitterName { get; init; } = string.Empty;
    public string CommitterEmail { get; init; } = string.Empty;
    public DateTimeOffset CommitterDate { get; init; }
    public IReadOnlyList<string> ParentHashes { get; init; } = [];
    public bool IsSigned { get; init; }
    public string SignatureStatus { get; init; } = string.Empty;

    public string FullMessage => string.IsNullOrEmpty(Body) ? Subject : $"{Subject}\n\n{Body}";
    public bool IsMergeCommit => ParentHashes.Count > 1;

    public override string ToString() => $"{ShortHash} {Subject}";
}
