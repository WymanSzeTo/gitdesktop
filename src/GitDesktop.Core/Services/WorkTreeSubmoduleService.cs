using GitDesktop.Core.Execution;
using GitDesktop.Core.Models;

namespace GitDesktop.Core.Services;

/// <summary>
/// Service for worktree and submodule operations.
/// </summary>
public sealed class WorkTreeSubmoduleService
{
    private readonly IGitExecutor _git;

    public WorkTreeSubmoduleService(IGitExecutor git)
    {
        _git = git;
    }

    // ── Worktrees ─────────────────────────────────────────────────────────────

    /// <summary>List worktrees.</summary>
    public async Task<IReadOnlyList<WorkTree>> ListWorkTreesAsync(string repoPath, CancellationToken ct = default)
    {
        var result = await _git.ExecuteAsync(repoPath, "worktree list --porcelain", ct);
        if (!result.Success) return [];

        var worktrees = new List<WorkTree>();
        WorkTree? current = null;
        bool isFirst = true;

        foreach (var line in result.Output.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (current != null)
                {
                    worktrees.Add(current);
                    current = null;
                }
                continue;
            }

            if (line.StartsWith("worktree "))
            {
                current = new WorkTree
                {
                    Path = line["worktree ".Length..].Trim(),
                    IsMainWorktree = isFirst,
                };
                isFirst = false;
            }
            else if (current != null)
            {
                if (line.StartsWith("HEAD "))
                {
                    current = current with { HeadHash = line["HEAD ".Length..].Trim() };
                }
                else if (line.StartsWith("branch "))
                {
                    current = current with { Branch = line["branch ".Length..].Trim() };
                }
                else if (line == "bare")
                {
                    current = current with { IsBare = true };
                }
                else if (line == "detached")
                {
                    current = current with { IsDetached = true };
                }
                else if (line.StartsWith("locked"))
                {
                    var reason = line.Length > "locked".Length ? line["locked".Length..].Trim() : null;
                    current = current with { Status = WorkTreeStatus.Locked, LockReason = reason };
                }
                else if (line.StartsWith("prunable"))
                {
                    current = current with { Status = WorkTreeStatus.Prunable };
                }
            }
        }
        if (current != null) worktrees.Add(current);
        return worktrees;
    }

    /// <summary>Add a worktree.</summary>
    public Task<GitResult> AddWorkTreeAsync(string repoPath, string path, string? branch = null, string? commitish = null, CancellationToken ct = default)
    {
        var args = $"worktree add \"{path}\"";
        if (branch != null) args += $" -b \"{branch}\"";
        if (commitish != null) args += $" \"{commitish}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Remove a worktree.</summary>
    public Task<GitResult> RemoveWorkTreeAsync(string repoPath, string path, bool force = false, CancellationToken ct = default)
    {
        var args = $"worktree remove \"{path}\"";
        if (force) args += " --force";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Prune stale worktree entries.</summary>
    public Task<GitResult> PruneWorkTreesAsync(string repoPath, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, "worktree prune", ct);

    // ── Submodules ─────────────────────────────────────────────────────────────

    /// <summary>List submodules.</summary>
    public async Task<IReadOnlyList<Submodule>> ListSubmodulesAsync(string repoPath, CancellationToken ct = default)
    {
        var result = await _git.ExecuteAsync(repoPath, "submodule status --recursive", ct);
        if (!result.Success) return [];

        var submodules = new List<Submodule>();
        foreach (var line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length < 2) continue;
            var statusChar = line[0];
            var rest = line[1..].Trim();
            var parts = rest.Split(' ');
            var hash = parts.Length > 0 ? parts[0] : string.Empty;
            var path = parts.Length > 1 ? parts[1] : string.Empty;

            var status = statusChar switch
            {
                '-' => SubmoduleStatus.Uninitialized,
                '+' => SubmoduleStatus.Modified,
                'U' => SubmoduleStatus.Conflict,
                _ => SubmoduleStatus.UpToDate,
            };

            submodules.Add(new Submodule
            {
                Name = path,
                Path = path,
                CommitHash = hash,
                Status = status,
            });
        }
        return submodules;
    }

    /// <summary>Add a submodule.</summary>
    public Task<GitResult> AddSubmoduleAsync(string repoPath, string url, string? path = null, string? branch = null, CancellationToken ct = default)
    {
        var args = $"submodule add \"{url}\"";
        if (branch != null) args += $" -b \"{branch}\"";
        if (path != null) args += $" \"{path}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Initialize submodules.</summary>
    public Task<GitResult> InitSubmodulesAsync(string repoPath, bool recursive = false, CancellationToken ct = default)
    {
        var args = "submodule init";
        if (recursive) args += " --recursive";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Update submodules.</summary>
    public Task<GitResult> UpdateSubmodulesAsync(string repoPath, bool recursive = false, bool init = false, CancellationToken ct = default)
    {
        var args = "submodule update";
        if (init) args += " --init";
        if (recursive) args += " --recursive";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Sync submodule URLs.</summary>
    public Task<GitResult> SyncSubmodulesAsync(string repoPath, bool recursive = false, CancellationToken ct = default)
    {
        var args = "submodule sync";
        if (recursive) args += " --recursive";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Deinitialize a submodule.</summary>
    public Task<GitResult> DeinitSubmoduleAsync(string repoPath, string path, bool force = false, CancellationToken ct = default)
    {
        var args = $"submodule deinit \"{path}\"";
        if (force) args += " --force";
        return _git.ExecuteAsync(repoPath, args, ct);
    }
}
