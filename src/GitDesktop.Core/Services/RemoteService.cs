using GitDesktop.Core.Execution;
using GitDesktop.Core.Models;

namespace GitDesktop.Core.Services;

/// <summary>
/// Service for remote management, fetch, pull, and push.
/// </summary>
public sealed class RemoteService
{
    private readonly IGitExecutor _git;

    public RemoteService(IGitExecutor git)
    {
        _git = git;
    }

    // ── Remote Configuration ─────────────────────────────────────────────────

    /// <summary>List configured remotes.</summary>
    public async Task<IReadOnlyList<Remote>> ListRemotesAsync(string repoPath, CancellationToken ct = default)
    {
        var result = await _git.ExecuteAsync(repoPath, "remote -v", ct);
        if (!result.Success) return [];

        var remotes = new Dictionary<string, (string? fetch, string? push)>(StringComparer.Ordinal);
        foreach (var line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t');
            if (parts.Length < 2) continue;
            var name = parts[0].Trim();
            var urlAndType = parts[1].Trim();
            var isPush = urlAndType.EndsWith("(push)");
            var url = urlAndType.Split(' ')[0];

            if (!remotes.TryGetValue(name, out var entry)) entry = (null, null);
            remotes[name] = isPush ? (entry.fetch, url) : (url, entry.push);
        }

        return remotes.Select(kv => new Remote
        {
            Name = kv.Key,
            FetchUrl = kv.Value.fetch ?? string.Empty,
            PushUrl = kv.Value.push,
        }).ToList();
    }

    /// <summary>Add a remote.</summary>
    public Task<GitResult> AddAsync(string repoPath, string name, string url, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"remote add \"{name}\" \"{url}\"", ct);

    /// <summary>Remove a remote.</summary>
    public Task<GitResult> RemoveAsync(string repoPath, string name, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"remote remove \"{name}\"", ct);

    /// <summary>Rename a remote.</summary>
    public Task<GitResult> RenameAsync(string repoPath, string oldName, string newName, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"remote rename \"{oldName}\" \"{newName}\"", ct);

    /// <summary>Set the fetch URL of a remote.</summary>
    public Task<GitResult> SetUrlAsync(string repoPath, string name, string url, bool push = false, CancellationToken ct = default)
    {
        var args = push
            ? $"remote set-url --push \"{name}\" \"{url}\""
            : $"remote set-url \"{name}\" \"{url}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    // ── Fetch ────────────────────────────────────────────────────────────────

    /// <summary>Fetch from all remotes or a specific remote.</summary>
    public Task<GitResult> FetchAsync(
        string repoPath,
        string? remote = null,
        bool prune = false,
        bool all = false,
        CancellationToken ct = default)
    {
        var args = all ? "fetch --all" : (remote != null ? $"fetch \"{remote}\"" : "fetch");
        if (prune) args += " --prune";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    // ── Pull ─────────────────────────────────────────────────────────────────

    public enum PullStrategy { Merge, Rebase, FastForwardOnly }

    /// <summary>Pull from a remote.</summary>
    public Task<GitResult> PullAsync(
        string repoPath,
        string? remote = null,
        PullStrategy strategy = PullStrategy.Merge,
        bool autoStash = false,
        CancellationToken ct = default)
    {
        var args = remote != null ? $"pull \"{remote}\"" : "pull";
        args += strategy switch
        {
            PullStrategy.Rebase => " --rebase",
            PullStrategy.FastForwardOnly => " --ff-only",
            _ => " --no-rebase",
        };
        if (autoStash) args += " --autostash";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    // ── Push ─────────────────────────────────────────────────────────────────

    /// <summary>Push to a remote.</summary>
    public Task<GitResult> PushAsync(
        string repoPath,
        string? remote = null,
        string? branch = null,
        bool force = false,
        bool forceWithLease = false,
        bool setUpstream = false,
        bool tags = false,
        CancellationToken ct = default)
    {
        var args = remote != null ? $"push \"{remote}\"" : "push";
        if (branch != null) args += $" \"{branch}\"";
        if (force) args += " --force";
        else if (forceWithLease) args += " --force-with-lease";
        if (setUpstream) args += " -u";
        if (tags) args += " --tags";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Push all tags to a remote.</summary>
    public Task<GitResult> PushTagsAsync(string repoPath, string? remote = null, CancellationToken ct = default)
    {
        var args = remote != null ? $"push \"{remote}\" --tags" : "push --tags";
        return _git.ExecuteAsync(repoPath, args, ct);
    }
}
