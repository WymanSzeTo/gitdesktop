namespace GitDesktop.Core.Models;

/// <summary>
/// Represents a git config entry.
/// </summary>
public sealed class ConfigEntry
{
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public ConfigScope Scope { get; init; }
}

public enum ConfigScope { System, Global, Local }

/// <summary>
/// Common git configuration keys.
/// </summary>
public static class ConfigKeys
{
    public const string UserName = "user.name";
    public const string UserEmail = "user.email";
    public const string UserSigningKey = "user.signingkey";
    public const string CommitGpgSign = "commit.gpgsign";
    public const string CoreAutoCrlf = "core.autocrlf";
    public const string CoreEditor = "core.editor";
    public const string PullRebase = "pull.rebase";
    public const string InitDefaultBranch = "init.defaultBranch";
    public const string RemoteOriginUrl = "remote.origin.url";
    public const string BranchAutoSetupMerge = "branch.autoSetupMerge";
}
