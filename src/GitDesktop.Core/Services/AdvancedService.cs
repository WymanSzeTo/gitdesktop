using GitDesktop.Core.Execution;

namespace GitDesktop.Core.Services;

/// <summary>
/// Service for advanced/plumbing git commands.
/// </summary>
public sealed class AdvancedService
{
    private readonly IGitExecutor _git;

    public AdvancedService(IGitExecutor git)
    {
        _git = git;
    }

    /// <summary>Run git cat-file on an object.</summary>
    public Task<GitResult> CatFileAsync(string repoPath, string objectRef, string type = "-p", CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"cat-file {type} \"{objectRef}\"", ct);

    /// <summary>Run git ls-tree.</summary>
    public Task<GitResult> LsTreeAsync(string repoPath, string treeish, bool recursive = false, CancellationToken ct = default)
    {
        var args = recursive ? $"ls-tree -r \"{treeish}\"" : $"ls-tree \"{treeish}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Run git hash-object.</summary>
    public Task<GitResult> HashObjectAsync(string repoPath, string filePath, bool write = false, CancellationToken ct = default)
    {
        var args = write ? $"hash-object -w \"{filePath}\"" : $"hash-object \"{filePath}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Run git update-ref.</summary>
    public Task<GitResult> UpdateRefAsync(string repoPath, string refName, string newValue, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"update-ref \"{refName}\" \"{newValue}\"", ct);

    /// <summary>Run git rev-parse.</summary>
    public Task<GitResult> RevParseAsync(string repoPath, string expression, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"rev-parse \"{expression}\"", ct);

    /// <summary>Run git rev-list.</summary>
    public Task<GitResult> RevListAsync(string repoPath, string range, int? limit = null, CancellationToken ct = default)
    {
        var args = $"rev-list \"{range}\"";
        if (limit.HasValue) args += $" -n {limit.Value}";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Run git describe.</summary>
    public Task<GitResult> DescribeAsync(string repoPath, string? commitish = null, bool tags = false, CancellationToken ct = default)
    {
        var args = "describe";
        if (tags) args += " --tags";
        if (commitish != null) args += $" \"{commitish}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Run git shortlog.</summary>
    public Task<GitResult> ShortlogAsync(string repoPath, string? range = null, bool summary = true, CancellationToken ct = default)
    {
        var args = "shortlog";
        if (summary) args += " -s";
        if (range != null) args += $" \"{range}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Run git pack-objects.</summary>
    public Task<GitResult> PackObjectsAsync(string repoPath, string baseName, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"pack-objects \"{baseName}\"", ct);

    /// <summary>Run git update-index.</summary>
    public Task<GitResult> UpdateIndexAsync(string repoPath, string args, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"update-index {args}", ct);

    /// <summary>Run arbitrary git command (escape hatch for full CLI coverage).</summary>
    public Task<GitResult> RunRawAsync(string repoPath, string arguments, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, arguments, ct);
}
