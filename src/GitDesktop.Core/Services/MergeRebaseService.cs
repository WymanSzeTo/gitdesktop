using GitDesktop.Core.Execution;

namespace GitDesktop.Core.Services;

/// <summary>
/// Service for merge, rebase, cherry-pick, revert, and reset operations.
/// </summary>
public sealed class MergeRebaseService
{
    private readonly IGitExecutor _git;

    public MergeRebaseService(IGitExecutor git)
    {
        _git = git;
    }

    // ── Merge ─────────────────────────────────────────────────────────────────

    /// <summary>Merge a branch into the current branch.</summary>
    public Task<GitResult> MergeAsync(
        string repoPath,
        string branchOrCommit,
        string? message = null,
        bool fastForwardOnly = false,
        bool noFastForward = false,
        bool sign = false,
        CancellationToken ct = default)
    {
        var args = $"merge \"{branchOrCommit}\"";
        if (fastForwardOnly) args += " --ff-only";
        else if (noFastForward) args += " --no-ff";
        if (message != null) args += $" -m \"{message}\"";
        if (sign) args += " -S";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Abort an in-progress merge.</summary>
    public Task<GitResult> MergeAbortAsync(string repoPath, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, "merge --abort", ct);

    // ── Rebase ────────────────────────────────────────────────────────────────

    /// <summary>Start a rebase onto a target.</summary>
    public Task<GitResult> RebaseAsync(
        string repoPath,
        string onto,
        bool interactive = false,
        bool autoSquash = false,
        bool autoStash = false,
        CancellationToken ct = default)
    {
        var args = $"rebase \"{onto}\"";
        if (interactive) args += " -i";
        if (autoSquash) args += " --autosquash";
        if (autoStash) args += " --autostash";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Continue a paused rebase.</summary>
    public Task<GitResult> RebaseContinueAsync(string repoPath, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, "rebase --continue", ct);

    /// <summary>Skip the current commit during rebase.</summary>
    public Task<GitResult> RebaseSkipAsync(string repoPath, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, "rebase --skip", ct);

    /// <summary>Abort an in-progress rebase.</summary>
    public Task<GitResult> RebaseAbortAsync(string repoPath, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, "rebase --abort", ct);

    // ── Cherry-pick ───────────────────────────────────────────────────────────

    /// <summary>Cherry-pick one or more commits.</summary>
    public Task<GitResult> CherryPickAsync(string repoPath, IEnumerable<string> commitHashes, bool noCommit = false, CancellationToken ct = default)
    {
        var commits = string.Join(" ", commitHashes.Select(h => $"\"{h}\""));
        var args = $"cherry-pick {commits}";
        if (noCommit) args += " -n";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Continue cherry-pick after conflict resolution.</summary>
    public Task<GitResult> CherryPickContinueAsync(string repoPath, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, "cherry-pick --continue", ct);

    /// <summary>Abort cherry-pick.</summary>
    public Task<GitResult> CherryPickAbortAsync(string repoPath, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, "cherry-pick --abort", ct);

    // ── Revert ────────────────────────────────────────────────────────────────

    /// <summary>Revert a commit.</summary>
    public Task<GitResult> RevertAsync(
        string repoPath,
        string commitHash,
        bool noCommit = false,
        string? message = null,
        CancellationToken ct = default)
    {
        var args = $"revert \"{commitHash}\"";
        if (noCommit) args += " -n";
        if (message != null) args += $" -m \"{message}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    public enum ResetMode { Soft, Mixed, Hard }

    /// <summary>Reset HEAD to a commit.</summary>
    public Task<GitResult> ResetAsync(string repoPath, string target, ResetMode mode = ResetMode.Mixed, CancellationToken ct = default)
    {
        var modeFlag = mode switch
        {
            ResetMode.Soft => "--soft",
            ResetMode.Hard => "--hard",
            _ => "--mixed",
        };
        return _git.ExecuteAsync(repoPath, $"reset {modeFlag} \"{target}\"", ct);
    }

    // ── Conflict resolution ───────────────────────────────────────────────────

    /// <summary>Mark a conflicted file as resolved.</summary>
    public Task<GitResult> MarkResolvedAsync(string repoPath, string filePath, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"add -- \"{filePath}\"", ct);

    /// <summary>Launch configured mergetool for conflict resolution.</summary>
    public Task<GitResult> MergeToolAsync(string repoPath, string? filePath = null, CancellationToken ct = default)
    {
        var args = filePath != null ? $"mergetool \"{filePath}\"" : "mergetool";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Checkout ours/theirs version of a conflicted file.</summary>
    public Task<GitResult> CheckoutConflictSideAsync(string repoPath, string filePath, bool ours, CancellationToken ct = default)
    {
        var side = ours ? "--ours" : "--theirs";
        return _git.ExecuteAsync(repoPath, $"checkout {side} -- \"{filePath}\"", ct);
    }
}
