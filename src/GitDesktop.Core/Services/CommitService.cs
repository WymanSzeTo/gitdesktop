using GitDesktop.Core.Execution;
using GitDesktop.Core.Models;

namespace GitDesktop.Core.Services;

/// <summary>
/// Service for staging, committing, stashing, and cleaning operations.
/// </summary>
public sealed class CommitService
{
    private readonly IGitExecutor _git;

    public CommitService(IGitExecutor git)
    {
        _git = git;
    }

    // ── Staging ──────────────────────────────────────────────────────────────

    /// <summary>Stage file(s). Pass "." to stage all.</summary>
    public Task<GitResult> StageAsync(string repoPath, string pathSpec, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"add -- \"{pathSpec}\"", ct);

    /// <summary>Stage all changes.</summary>
    public Task<GitResult> StageAllAsync(string repoPath, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, "add -A", ct);

    /// <summary>Unstage file(s) (reset HEAD).</summary>
    public Task<GitResult> UnstageAsync(string repoPath, string pathSpec, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"reset HEAD -- \"{pathSpec}\"", ct);

    /// <summary>Interactive patch staging.</summary>
    public Task<GitResult> StagePatchAsync(string repoPath, string pathSpec, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"add -p -- \"{pathSpec}\"", ct);

    // ── Committing ───────────────────────────────────────────────────────────

    /// <summary>Create a commit with the given message.</summary>
    public Task<GitResult> CommitAsync(
        string repoPath,
        string message,
        bool sign = false,
        bool allowEmpty = false,
        CancellationToken ct = default)
    {
        var args = $"commit -m \"{EscapeMessage(message)}\"";
        if (sign) args += " -S";
        if (allowEmpty) args += " --allow-empty";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Amend the last commit.</summary>
    public Task<GitResult> AmendAsync(
        string repoPath,
        string? message = null,
        bool noEdit = false,
        CancellationToken ct = default)
    {
        var args = "commit --amend";
        if (noEdit) args += " --no-edit";
        else if (message != null) args += $" -m \"{EscapeMessage(message)}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    // ── Stash ─────────────────────────────────────────────────────────────────

    /// <summary>Create a stash.</summary>
    public Task<GitResult> StashPushAsync(
        string repoPath,
        string? message = null,
        bool includeUntracked = false,
        bool includeIgnored = false,
        CancellationToken ct = default)
    {
        var args = "stash push";
        if (includeUntracked) args += " -u";
        if (includeIgnored) args += " -a";
        if (message != null) args += $" -m \"{EscapeMessage(message)}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>List all stashes.</summary>
    public async Task<IReadOnlyList<Stash>> StashListAsync(string repoPath, CancellationToken ct = default)
    {
        var result = await _git.ExecuteAsync(repoPath, "stash list --format=%gd|%gs|%H|%ai", ct);
        if (!result.Success) return [];

        var stashes = new List<Stash>();
        int index = 0;
        foreach (var line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('|');
            stashes.Add(new Stash
            {
                Index = index++,
                Ref = parts.Length > 0 ? parts[0] : $"stash@{{{index}}}",
                Message = parts.Length > 1 ? parts[1] : line,
                CommitHash = parts.Length > 2 ? parts[2] : string.Empty,
                Date = parts.Length > 3 && DateTimeOffset.TryParse(parts[3], out var dt) ? dt : DateTimeOffset.Now,
            });
        }
        return stashes;
    }

    /// <summary>Apply a stash (keep it in the list).</summary>
    public Task<GitResult> StashApplyAsync(string repoPath, int index = 0, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"stash apply stash@{{{index}}}", ct);

    /// <summary>Pop a stash (apply and remove).</summary>
    public Task<GitResult> StashPopAsync(string repoPath, int index = 0, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"stash pop stash@{{{index}}}", ct);

    /// <summary>Drop a stash.</summary>
    public Task<GitResult> StashDropAsync(string repoPath, int index = 0, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"stash drop stash@{{{index}}}", ct);

    /// <summary>Show stash diff.</summary>
    public Task<GitResult> StashShowAsync(string repoPath, int index = 0, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"stash show -p stash@{{{index}}}", ct);

    // ── Discard / Clean ───────────────────────────────────────────────────────

    /// <summary>Discard changes in working tree for a file.</summary>
    public Task<GitResult> DiscardAsync(string repoPath, string pathSpec, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"checkout -- \"{pathSpec}\"", ct);

    /// <summary>Restore a file using modern git restore.</summary>
    public Task<GitResult> RestoreAsync(
        string repoPath,
        string pathSpec,
        bool staged = false,
        string? source = null,
        CancellationToken ct = default)
    {
        var args = "restore";
        if (staged) args += " --staged";
        if (source != null) args += $" --source=\"{source}\"";
        args += $" -- \"{pathSpec}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Preview files that git clean would remove.</summary>
    public Task<GitResult> CleanDryRunAsync(string repoPath, bool directories = false, bool ignored = false, CancellationToken ct = default)
    {
        var args = "clean -n";
        if (directories) args += " -d";
        if (ignored) args += " -x";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Clean untracked files.</summary>
    public Task<GitResult> CleanAsync(string repoPath, bool directories = false, bool ignored = false, bool force = false, CancellationToken ct = default)
    {
        var args = "clean";
        if (force) args += " -f";
        if (directories) args += " -d";
        if (ignored) args += " -x";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    private static string EscapeMessage(string message)
        => message.Replace("\"", "\\\"").Replace("\n", "\\n");
}
