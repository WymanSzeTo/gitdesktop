namespace GitDesktop.Core.Models;

/// <summary>
/// Represents a stash entry.
/// </summary>
public sealed class Stash
{
    public int Index { get; init; }
    public string Ref { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string CommitHash { get; init; } = string.Empty;
    public DateTimeOffset Date { get; init; }

    public override string ToString() => $"stash@{{{Index}}}: {Message}";
}
