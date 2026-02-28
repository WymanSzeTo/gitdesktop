using GitDesktop.Core.Execution;
using GitDesktop.Core.Models;

namespace GitDesktop.Core.Services;

/// <summary>
/// Service for git configuration management.
/// </summary>
public sealed class ConfigService
{
    private readonly IGitExecutor _git;

    public ConfigService(IGitExecutor git)
    {
        _git = git;
    }

    /// <summary>Get a config value.</summary>
    public async Task<string?> GetAsync(string repoPath, string key, ConfigScope scope = ConfigScope.Local, CancellationToken ct = default)
    {
        var args = $"config {ScopeFlag(scope)} --get \"{key}\"";
        var result = await _git.ExecuteAsync(repoPath, args, ct);
        return result.Success ? result.Output.Trim() : null;
    }

    /// <summary>Set a config value.</summary>
    public Task<GitResult> SetAsync(string repoPath, string key, string value, ConfigScope scope = ConfigScope.Local, CancellationToken ct = default)
    {
        var args = $"config {ScopeFlag(scope)} \"{key}\" \"{value}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Unset a config value.</summary>
    public Task<GitResult> UnsetAsync(string repoPath, string key, ConfigScope scope = ConfigScope.Local, CancellationToken ct = default)
    {
        var args = $"config {ScopeFlag(scope)} --unset \"{key}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>List all config entries for a scope.</summary>
    public async Task<IReadOnlyList<ConfigEntry>> ListAsync(string repoPath, ConfigScope scope = ConfigScope.Local, CancellationToken ct = default)
    {
        var args = $"config {ScopeFlag(scope)} --list";
        var result = await _git.ExecuteAsync(repoPath, args, ct);
        if (!result.Success) return [];

        return result.Output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line =>
            {
                var eq = line.IndexOf('=');
                return eq >= 0
                    ? new ConfigEntry { Key = line[..eq], Value = line[(eq + 1)..], Scope = scope }
                    : new ConfigEntry { Key = line, Value = string.Empty, Scope = scope };
            })
            .ToList();
    }

    /// <summary>Edit config file in the default editor.</summary>
    public Task<GitResult> EditAsync(string repoPath, ConfigScope scope = ConfigScope.Local, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"config {ScopeFlag(scope)} --edit", ct);

    private static string ScopeFlag(ConfigScope scope) => scope switch
    {
        ConfigScope.System => "--system",
        ConfigScope.Global => "--global",
        _ => "--local",
    };
}
