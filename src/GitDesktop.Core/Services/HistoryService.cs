using GitDesktop.Core.Execution;
using GitDesktop.Core.Models;

namespace GitDesktop.Core.Services;

/// <summary>
/// Service for log, diff, blame, history, and reflog operations.
/// </summary>
public sealed class HistoryService
{
    private readonly IGitExecutor _git;

    public HistoryService(IGitExecutor git)
    {
        _git = git;
    }

    // ── Log ──────────────────────────────────────────────────────────────────

    /// <summary>Return commit log entries.</summary>
    public async Task<IReadOnlyList<Commit>> GetLogAsync(
        string repoPath,
        string? branch = null,
        string? author = null,
        string? pathSpec = null,
        int limit = 100,
        int skip = 0,
        string? since = null,
        string? until = null,
        CancellationToken ct = default)
    {
        const string sep = "\x1f";
        const string recordSep = "\x1e";
        var fmt = $"--pretty=format:%H{sep}%h{sep}%s{sep}%b{sep}%an{sep}%ae{sep}%ai{sep}%cn{sep}%ce{sep}%ci{sep}%P{recordSep}";
        var args = $"log {fmt} -n {limit} --skip {skip}";
        if (branch != null) args += $" \"{branch}\"";
        if (author != null) args += $" --author=\"{author}\"";
        if (since != null) args += $" --since=\"{since}\"";
        if (until != null) args += $" --until=\"{until}\"";
        if (pathSpec != null) args += $" -- \"{pathSpec}\"";

        var result = await _git.ExecuteAsync(repoPath, args, ct);
        if (!result.Success) return [];

        return result.Output
            .Split(recordSep, StringSplitOptions.RemoveEmptyEntries)
            .Select(record => ParseCommit(record, sep))
            .Where(c => c != null)
            .Cast<Commit>()
            .ToList();
    }

    /// <summary>Get details of a single commit.</summary>
    public async Task<Commit?> GetCommitAsync(string repoPath, string commitish, CancellationToken ct = default)
    {
        const string sep = "\x1f";
        var fmt = $"--pretty=format:%H{sep}%h{sep}%s{sep}%b{sep}%an{sep}%ae{sep}%ai{sep}%cn{sep}%ce{sep}%ci{sep}%P";
        var result = await _git.ExecuteAsync(repoPath, $"show {fmt} -s \"{commitish}\"", ct);
        return result.Success ? ParseCommit(result.Output, sep) : null;
    }

    /// <summary>Show raw output for a commit.</summary>
    public Task<GitResult> ShowAsync(string repoPath, string commitish, CancellationToken ct = default)
        => _git.ExecuteAsync(repoPath, $"show \"{commitish}\"", ct);

    // ── Diff ─────────────────────────────────────────────────────────────────

    /// <summary>Get diff of working tree vs index.</summary>
    public Task<GitResult> DiffAsync(string repoPath, string? pathSpec = null, bool cached = false, bool ignoreWhitespace = false, CancellationToken ct = default)
    {
        var args = "diff";
        if (cached) args += " --cached";
        if (ignoreWhitespace) args += " -w";
        if (pathSpec != null) args += $" -- \"{pathSpec}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    /// <summary>Get diff between two commits/refs.</summary>
    public Task<GitResult> DiffRefsAsync(string repoPath, string from, string to, string? pathSpec = null, CancellationToken ct = default)
    {
        var args = $"diff \"{from}\" \"{to}\"";
        if (pathSpec != null) args += $" -- \"{pathSpec}\"";
        return _git.ExecuteAsync(repoPath, args, ct);
    }

    // ── Blame ─────────────────────────────────────────────────────────────────

    /// <summary>Run git blame on a file.</summary>
    public async Task<BlameResult> BlameAsync(string repoPath, string filePath, string? commit = null, CancellationToken ct = default)
    {
        var target = commit ?? "HEAD";
        var result = await _git.ExecuteAsync(repoPath, $"blame --porcelain \"{commit ?? string.Empty}\" -- \"{filePath}\"", ct);
        if (!result.Success) return new BlameResult { FilePath = filePath };

        var lines = new List<BlameLine>();
        var commitMeta = new Dictionary<string, (string author, string email, DateTimeOffset date, string summary)>(StringComparer.Ordinal);
        var rawLines = result.Output.Split('\n');
        int i = 0;
        while (i < rawLines.Length)
        {
            var header = rawLines[i];
            if (header.Length < 40) { i++; continue; }

            var hash = header[..40];
            var headerParts = header.Split(' ');
            int lineNum = headerParts.Length >= 3 && int.TryParse(headerParts[2], out var ln) ? ln : 0;
            string author = string.Empty, email = string.Empty, summary = string.Empty;
            DateTimeOffset date = DateTimeOffset.MinValue;
            string? lineContent = null;

            i++;
            while (i < rawLines.Length && !rawLines[i].StartsWith('\t'))
            {
                var meta = rawLines[i];
                if (meta.StartsWith("author ")) author = meta["author ".Length..];
                else if (meta.StartsWith("author-mail ")) email = meta["author-mail ".Length..].Trim('<', '>');
                else if (meta.StartsWith("author-time ") && long.TryParse(meta["author-time ".Length..], out var ts))
                    date = DateTimeOffset.FromUnixTimeSeconds(ts);
                else if (meta.StartsWith("summary ")) summary = meta["summary ".Length..];
                i++;
            }
            if (i < rawLines.Length && rawLines[i].StartsWith('\t'))
            {
                lineContent = rawLines[i][1..];
                i++;
            }

            commitMeta[hash] = (author, email, date, summary);
            if (lineContent != null)
                lines.Add(new BlameLine
                {
                    LineNumber = lineNum,
                    Content = lineContent,
                    CommitHash = hash,
                    AuthorName = author,
                    AuthorEmail = email,
                    AuthorDate = date,
                    Summary = summary,
                });
        }

        return new BlameResult { FilePath = filePath, Commit = target, Lines = lines };
    }

    // ── Reflog ────────────────────────────────────────────────────────────────

    /// <summary>Get reflog entries.</summary>
    public async Task<IReadOnlyList<ReflogEntry>> GetReflogAsync(string repoPath, string? @ref = null, int limit = 100, CancellationToken ct = default)
    {
        var target = @ref ?? "HEAD";
        var result = await _git.ExecuteAsync(repoPath, $"reflog show --format=%H|%gd|%gs|%ai|%an -n {limit} \"{target}\"", ct);
        if (!result.Success) return [];

        var entries = new List<ReflogEntry>();
        int index = 0;
        foreach (var line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('|');
            entries.Add(new ReflogEntry
            {
                Index = index++,
                Hash = parts.Length > 0 ? parts[0].Trim() : string.Empty,
                Ref = parts.Length > 1 ? parts[1].Trim() : string.Empty,
                Message = parts.Length > 2 ? parts[2].Trim() : string.Empty,
                Date = parts.Length > 3 && DateTimeOffset.TryParse(parts[3], out var dt) ? dt : DateTimeOffset.MinValue,
                AuthorName = parts.Length > 4 ? parts[4].Trim() : string.Empty,
            });
        }
        return entries;
    }

    // ── Search ────────────────────────────────────────────────────────────────

    /// <summary>Run git grep.</summary>
    public async Task<GrepResult> GrepAsync(
        string repoPath,
        string pattern,
        string? @ref = null,
        string? pathSpec = null,
        bool useRegex = false,
        bool ignoreCase = false,
        CancellationToken ct = default)
    {
        var args = "grep -n";
        if (useRegex) args += " -E";
        if (ignoreCase) args += " -i";
        args += $" \"{pattern}\"";
        if (@ref != null) args += $" \"{@ref}\"";
        if (pathSpec != null) args += $" -- \"{pathSpec}\"";

        var result = await _git.ExecuteAsync(repoPath, args, ct);
        var matches = new List<GrepMatch>();

        foreach (var line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var colonIdx = line.IndexOf(':');
            if (colonIdx < 0) continue;
            var filePart = line[..colonIdx];
            var rest = line[(colonIdx + 1)..];
            var colonIdx2 = rest.IndexOf(':');
            if (colonIdx2 < 0)
            {
                matches.Add(new GrepMatch { FilePath = filePart, LineContent = rest, Ref = @ref ?? "HEAD" });
                continue;
            }
            var lineNumStr = rest[..colonIdx2];
            var content = rest[(colonIdx2 + 1)..];
            matches.Add(new GrepMatch
            {
                FilePath = filePart,
                LineNumber = int.TryParse(lineNumStr, out var ln) ? ln : 0,
                LineContent = content,
                Ref = @ref ?? "HEAD",
            });
        }

        return new GrepResult { Pattern = pattern, Matches = matches };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Commit? ParseCommit(string record, string sep)
    {
        var parts = record.Trim().Split(sep);
        if (parts.Length < 10) return null;

        return new Commit
        {
            Hash = parts[0].Trim(),
            ShortHash = parts[1].Trim(),
            Subject = parts[2].Trim(),
            Body = parts[3].Trim(),
            AuthorName = parts[4].Trim(),
            AuthorEmail = parts[5].Trim(),
            AuthorDate = DateTimeOffset.TryParse(parts[6].Trim(), out var ad) ? ad : DateTimeOffset.MinValue,
            CommitterName = parts[7].Trim(),
            CommitterEmail = parts[8].Trim(),
            CommitterDate = DateTimeOffset.TryParse(parts[9].Trim(), out var cd) ? cd : DateTimeOffset.MinValue,
            ParentHashes = parts.Length > 10
                ? parts[10].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                : [],
        };
    }
}
