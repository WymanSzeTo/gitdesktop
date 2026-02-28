using GitDesktop.Core.Execution;
using GitDesktop.Core.Models;

namespace GitDesktop.Core.Services;

/// <summary>
/// Service for bisect operations.
/// </summary>
public sealed class BisectService
{
    private readonly IGitExecutor _git;

    public BisectService(IGitExecutor git)
    {
        _git = git;
    }

    /// <summary>Start a bisect session.</summary>
    public Task<GitResult> StartAsync(string repoPath, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, "bisect start", ct);

    /// <summary>Mark the current commit as good.</summary>
    public Task<GitResult> MarkGoodAsync(string repoPath, string? commit = null, CancellationToken ct = default)
    {
        var args = commit != null ? $"bisect good \"{commit}\"" : "bisect good";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Mark the current commit as bad.</summary>
    public Task<GitResult> MarkBadAsync(string repoPath, string? commit = null, CancellationToken ct = default)
    {
        var args = commit != null ? $"bisect bad \"{commit}\"" : "bisect bad";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Skip the current commit.</summary>
    public Task<GitResult> SkipAsync(string repoPath, string? commit = null, CancellationToken ct = default)
    {
        var args = commit != null ? $"bisect skip \"{commit}\"" : "bisect skip";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Reset (end) the bisect session.</summary>
    public Task<GitResult> ResetAsync(string repoPath, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, "bisect reset", ct);

    /// <summary>Run an automated test command during bisect.</summary>
    public Task<GitResult> RunAsync(string repoPath, string command, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"bisect run {command}", ct);

    /// <summary>Get the current bisect log.</summary>
    public Task<GitResult> GetLogAsync(string repoPath, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, "bisect log", ct);
}
