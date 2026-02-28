namespace GitDesktop.Core.Models;

public enum TagType { Lightweight, Annotated }

/// <summary>
/// Represents a Git tag.
/// </summary>
public sealed class Tag
{
    public string Name { get; init; } = string.Empty;
    public TagType Type { get; init; }
    public string TargetHash { get; init; } = string.Empty;
    public string? Message { get; init; }
    public string? TaggerName { get; init; }
    public string? TaggerEmail { get; init; }
    public DateTimeOffset? TaggerDate { get; init; }

    public bool IsAnnotated => Type == TagType.Annotated;

    public override string ToString() => Name;
}
