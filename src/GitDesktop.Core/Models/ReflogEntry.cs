namespace GitDesktop.Core.Models;

/// <summary>
/// Represents a reflog entry.
/// </summary>
public sealed class ReflogEntry
{
    public int Index { get; init; }
    public string Hash { get; init; } = string.Empty;
    public string PreviousHash { get; init; } = string.Empty;
    public string Ref { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public DateTimeOffset Date { get; init; }

    public override string ToString() => $"{Hash} {Ref}@{{{Index}}}: {Action}: {Message}";
}
