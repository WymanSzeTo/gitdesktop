using GitDesktop.Core.Execution;
using GitDesktop.Core.Models;

namespace GitDesktop.Core.Services;

/// <summary>
/// Service for branch and tag management.
/// </summary>
public sealed class BranchService
{
    private readonly IGitExecutor _git;

    public BranchService(IGitExecutor git)
    {
        _git = git;
    }

    // ── Branches ─────────────────────────────────────────────────────────────

    /// <summary>List local and remote branches.</summary>
    public async Task<IReadOnlyList<Branch>> ListBranchesAsync(string repoPath, bool includeRemotes = true, CancellationToken ct = default)
    {
        var format = "%(refname:short)|%(objectname:short)|%(HEAD)|%(upstream:short)|%(upstream:track)";
        var args = includeRemotes
            ? $"branch -a --format=\"{format}\""
            : $"branch --format=\"{format}\"";

        var result = await _git.ExecuteAsync(repoPath, args, ct);
        if (!result.Success) return [];

        var branches = new List<Branch>();
        foreach (var line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('|');
            if (parts.Length < 3) continue;
            var name = parts[0].Trim().TrimStart('*').Trim();
            var isRemote = name.StartsWith("remotes/");
            var displayName = isRemote ? name["remotes/".Length..] : name;
            var track = parts.Length > 4 ? parts[4] : string.Empty;
            ParseAheadBehind(track, out var ahead, out var behind);

            branches.Add(new Branch
            {
                Name = displayName,
                FullName = name,
                Type = isRemote ? BranchType.Remote : BranchType.Local,
                TipHash = parts[1].Trim(),
                IsCurrentBranch = parts[2].Trim() == "*",
                UpstreamName = parts.Length > 3 && !string.IsNullOrEmpty(parts[3]) ? parts[3].Trim() : null,
                AheadCount = ahead,
                BehindCount = behind,
            });
        }
        return branches;
    }

    /// <summary>Create a branch.</summary>
    public Task<GitResult> CreateAsync(string repoPath, string branchName, string? startPoint = null, CancellationToken ct = default)
    {
        var args = startPoint != null
            ? $"branch \"{branchName}\" \"{startPoint}\""
            : $"branch \"{branchName}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Switch to a branch.</summary>
    public Task<GitResult> SwitchAsync(string repoPath, string branchName, bool createIfNotExists = false, CancellationToken ct = default)
    {
        var args = createIfNotExists
            ? $"switch -C \"{branchName}\""
            : $"switch \"{branchName}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Checkout a branch or commit (legacy).</summary>
    public Task<GitResult> CheckoutAsync(string repoPath, string target, bool newBranch = false, CancellationToken ct = default)
    {
        var args = newBranch ? $"checkout -b \"{target}\"" : $"checkout \"{target}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Rename a local branch.</summary>
    public Task<GitResult> RenameAsync(string repoPath, string oldName, string newName, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"branch -m \"{oldName}\" \"{newName}\"", ct);

    /// <summary>Delete a branch (safe or forced).</summary>
    public Task<GitResult> DeleteAsync(string repoPath, string branchName, bool force = false, CancellationToken ct = default)
    {
        var flag = force ? "-D" : "-d";
        return _git.ExecuteAsync(repoPath, $"branch {flag} \"{branchName}\"", ct);
    }

    /// <summary>Set upstream for a branch.</summary>
    public Task<GitResult> SetUpstreamAsync(string repoPath, string branchName, string upstream, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"branch --set-upstream-to=\"{upstream}\" \"{branchName}\"", ct);

    /// <summary>Unset upstream for a branch.</summary>
    public Task<GitResult> UnsetUpstreamAsync(string repoPath, string branchName, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"branch --unset-upstream \"{branchName}\"", ct);

    // ── Tags ─────────────────────────────────────────────────────────────────

    /// <summary>List all tags.</summary>
    public async Task<IReadOnlyList<Tag>> ListTagsAsync(string repoPath, CancellationToken ct = default)
    {
        var result = await _git.ExecuteAsync(repoPath, "tag -l --format=%(refname:short)|%(objecttype)|%(*objectname)|%(objectname)|%(taggername)|%(taggeremail)|%(taggerdate:iso)|%(contents:subject)", ct);
        if (!result.Success) return [];

        var tags = new List<Tag>();
        foreach (var line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('|');
            var isAnnotated = parts.Length > 1 && parts[1].Trim() == "tag";
            tags.Add(new Tag
            {
                Name = parts.Length > 0 ? parts[0].Trim() : line,
                Type = isAnnotated ? TagType.Annotated : TagType.Lightweight,
                TargetHash = parts.Length > 2 && !string.IsNullOrEmpty(parts[2]) ? parts[2].Trim() : (parts.Length > 3 ? parts[3].Trim() : string.Empty),
                TaggerName = parts.Length > 4 && !string.IsNullOrEmpty(parts[4]) ? parts[4].Trim() : null,
                TaggerEmail = parts.Length > 5 && !string.IsNullOrEmpty(parts[5]) ? parts[5].Trim('<', '>', ' ') : null,
                TaggerDate = parts.Length > 6 && DateTimeOffset.TryParse(parts[6], out var dt) ? dt : null,
                Message = parts.Length > 7 ? parts[7].Trim() : null,
            });
        }
        return tags;
    }

    /// <summary>Create a lightweight tag.</summary>
    public Task<GitResult> CreateLightweightTagAsync(string repoPath, string tagName, string? commitish = null, CancellationToken ct = default)
    {
        var args = commitish != null ? $"tag \"{tagName}\" \"{commitish}\"" : $"tag \"{tagName}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Create an annotated tag.</summary>
    public Task<GitResult> CreateAnnotatedTagAsync(string repoPath, string tagName, string message, string? commitish = null, bool sign = false, CancellationToken ct = default)
    {
        var args = $"tag -a \"{tagName}\" -m \"{message}\"";
        if (sign) args += " -s";
        if (commitish != null) args += $" \"{commitish}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Delete a local tag.</summary>
    public Task<GitResult> DeleteTagAsync(string repoPath, string tagName, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"tag -d \"{tagName}\"", ct);

    /// <summary>Delete a remote tag.</summary>
    public Task<GitResult> DeleteRemoteTagAsync(string repoPath, string remote, string tagName, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"push \"{remote}\" --delete \"refs/tags/{tagName}\"", ct);

    private static void ParseAheadBehind(string track, out int ahead, out int behind)
    {
        ahead = behind = 0;
        if (string.IsNullOrEmpty(track)) return;
        var aheadMatch = System.Text.RegularExpressions.Regex.Match(track, @"ahead (\d+)");
        var behindMatch = System.Text.RegularExpressions.Regex.Match(track, @"behind (\d+)");
        if (aheadMatch.Success) int.TryParse(aheadMatch.Groups[1].Value, out ahead);
        if (behindMatch.Success) int.TryParse(behindMatch.Groups[1].Value, out behind);
    }
}
