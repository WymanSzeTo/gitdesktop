using GitDesktop.Core.Execution;
using GitDesktop.Core.Models;

namespace GitDesktop.Core.Services;

/// <summary>
/// Service for repository lifecycle operations: init, clone, open.
/// </summary>
public sealed class RepositoryService
{
    private readonly IGitExecutor _git;

    public RepositoryService(IGitExecutor git)
    {
        _git = git;
    }

    /// <summary>Initializes a new git repository at the given path.</summary>
    public async Task<GitResult> InitAsync(string path, bool bare = false, string? initialBranch = null, CancellationToken ct = default)
    {
        var args = "init";
        if (bare) args += " --bare";
        if (initialBranch != null) args += $" -b \"{initialBranch}\"";
        args += $" \"{path}\"";
        return await _git.ExecuteAsync(path, args, ct);
    }

    /// <summary>Clones a repository.</summary>
    public async Task<GitResult> CloneAsync(
        string url,
        string destination,
        int? depth = null,
        bool singleBranch = false,
        string? branch = null,
        bool recurseSubmodules = false,
        CancellationToken ct = default)
    {
        var args = $"clone \"{url}\" \"{destination}\"";
        if (depth.HasValue) args += $" --depth {depth.Value}";
        if (singleBranch) args += " --single-branch";
        if (branch != null) args += $" -b \"{branch}\"";
        if (recurseSubmodules) args += " --recurse-submodules";
        return await _git.ExecuteAsync(destination, args, ct);
    }

    /// <summary>Returns basic info about a repository.</summary>
    public async Task<Repository?> OpenAsync(string path, CancellationToken ct = default)
    {
        var revParse = await _git.ExecuteAsync(path, "rev-parse --git-dir --is-bare-repository --abbrev-ref HEAD", ct);
        if (!revParse.Success) return null;

        var lines = revParse.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var gitDir = lines.Length > 0 ? lines[0].Trim() : ".git";
        var isBare = lines.Length > 1 && lines[1].Trim() == "true";
        var defaultBranch = lines.Length > 2 ? lines[2].Trim() : "main";

        var versionResult = await _git.ExecuteAsync(path, "version", ct);
        var gitVersion = versionResult.Success ? versionResult.Output.Trim() : string.Empty;

        return new Repository
        {
            Path = path,
            GitDirectory = System.IO.Path.Combine(path, gitDir),
            IsBare = isBare,
            DefaultBranch = defaultBranch,
            GitVersion = gitVersion,
        };
    }

    /// <summary>Runs git status and returns structured output.</summary>
    public async Task<RepositoryStatus> GetStatusAsync(string path, CancellationToken ct = default)
    {
        var result = await _git.ExecuteAsync(path, "status --porcelain=v2 --branch", ct);
        return ParseStatus(result.Output);
    }

    /// <summary>Runs git fsck for integrity checking.</summary>
    public async Task<GitResult> FsckAsync(string path, CancellationToken ct = default)
        => await _git.ExecuteAsync(path, "fsck", ct);

    /// <summary>Runs git gc for garbage collection.</summary>
    public async Task<GitResult> GcAsync(string path, bool aggressive = false, bool auto = false, CancellationToken ct = default)
    {
        var args = "gc";
        if (aggressive) args += " --aggressive";
        if (auto) args += " --auto";
        return await _git.ExecuteAsync(path, args, ct);
    }

    /// <summary>Runs git count-objects.</summary>
    public async Task<GitResult> CountObjectsAsync(string path, bool verbose = false, CancellationToken ct = default)
    {
        var args = "count-objects";
        if (verbose) args += " -v";
        return await _git.ExecuteAsync(path, args, ct);
    }

    /// <summary>Runs git verify-pack.</summary>
    public async Task<GitResult> VerifyPackAsync(string path, string packFile, CancellationToken ct = default)
        => await _git.ExecuteAsync(path, $"verify-pack -v \"{packFile}\"", ct);

    private static RepositoryStatus ParseStatus(string output)
    {
        string currentBranch = string.Empty;
        string? upstreamBranch = null;
        int ahead = 0, behind = 0;
        bool isDetached = false;
        var entries = new List<StatusEntry>();

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("# branch.head "))
            {
                currentBranch = line["# branch.head ".Length..].Trim();
                isDetached = currentBranch == "(detached)";
            }
            else if (line.StartsWith("# branch.upstream "))
            {
                upstreamBranch = line["# branch.upstream ".Length..].Trim();
            }
            else if (line.StartsWith("# branch.ab "))
            {
                var ab = line["# branch.ab ".Length..].Trim().Split(' ');
                if (ab.Length == 2)
                {
                    int.TryParse(ab[0].TrimStart('+'), out ahead);
                    int.TryParse(ab[1].TrimStart('-'), out behind);
                }
            }
            else if (line.StartsWith("1 ") || line.StartsWith("2 "))
            {
                var parts = line.Split(' ');
                if (parts.Length >= 9)
                {
                    var xy = parts[1];
                    var filePath = parts.Length >= 9 ? string.Join(" ", parts[8..]) : parts[^1];
                    entries.Add(new StatusEntry
                    {
                        Path = filePath,
                        IndexStatus = ParseXyStatus(xy.Length > 0 ? xy[0] : '.'),
                        WorkTreeStatus = ParseXyStatus(xy.Length > 1 ? xy[1] : '.'),
                    });
                }
            }
            else if (line.StartsWith("? "))
            {
                entries.Add(new StatusEntry
                {
                    Path = line[2..].Trim(),
                    IndexStatus = FileStatusKind.Untracked,
                    WorkTreeStatus = FileStatusKind.Untracked,
                });
            }
            else if (line.StartsWith("u "))
            {
                var parts = line.Split(' ');
                var filePath = parts.Length >= 11 ? string.Join(" ", parts[10..]) : parts[^1];
                entries.Add(new StatusEntry
                {
                    Path = filePath,
                    IndexStatus = FileStatusKind.Unmerged,
                    WorkTreeStatus = FileStatusKind.Unmerged,
                });
            }
        }

        return new RepositoryStatus
        {
            CurrentBranch = currentBranch,
            UpstreamBranch = upstreamBranch,
            AheadCount = ahead,
            BehindCount = behind,
            IsDetachedHead = isDetached,
            Entries = entries,
        };
    }

    private static FileStatusKind ParseXyStatus(char c) => c switch
    {
        'M' => FileStatusKind.Modified,
        'A' => FileStatusKind.Added,
        'D' => FileStatusKind.Deleted,
        'R' => FileStatusKind.Renamed,
        'C' => FileStatusKind.Copied,
        'U' => FileStatusKind.Unmerged,
        '?' => FileStatusKind.Untracked,
        '!' => FileStatusKind.Ignored,
        _ => FileStatusKind.Modified,
    };
}
