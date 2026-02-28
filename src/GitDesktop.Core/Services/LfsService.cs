using GitDesktop.Core.Execution;

namespace GitDesktop.Core.Services;

/// <summary>
/// Service for LFS operations.
/// </summary>
public sealed class LfsService
{
    private readonly IGitExecutor _git;

    public LfsService(IGitExecutor git)
    {
        _git = git;
    }

    /// <summary>Check if LFS is installed and configured in the repository.</summary>
    public async Task<bool> IsInstalledAsync(string repoPath, CancellationToken ct = default)
    {
        var result = await _git.ExecuteAsync(repoPath, "lfs version", ct);
        return result.Success;
    }

    /// <summary>Install LFS hooks in the repository.</summary>
    public Task<GitResult> InstallAsync(string repoPath, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, "lfs install", ct);

    /// <summary>Track a file pattern with LFS.</summary>
    public Task<GitResult> TrackAsync(string repoPath, string pattern, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"lfs track \"{pattern}\"", ct);

    /// <summary>Untrack a file pattern from LFS.</summary>
    public Task<GitResult> UntrackAsync(string repoPath, string pattern, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"lfs untrack \"{pattern}\"", ct);

    /// <summary>List tracked patterns.</summary>
    public Task<GitResult> ListTrackedAsync(string repoPath, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, "lfs track", ct);

    /// <summary>List LFS files.</summary>
    public Task<GitResult> ListFilesAsync(string repoPath, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, "lfs ls-files", ct);

    /// <summary>Fetch LFS objects.</summary>
    public Task<GitResult> FetchAsync(string repoPath, bool all = false, CancellationToken ct = default)
    {
        var args = all ? "lfs fetch --all" : "lfs fetch";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Prune old LFS objects.</summary>
    public Task<GitResult> PruneAsync(string repoPath, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, "lfs prune", ct);

    /// <summary>Get LFS status.</summary>
    public Task<GitResult> StatusAsync(string repoPath, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, "lfs status", ct);
}
