using GitDesktop.Core;

namespace GitDesktop.Cli;

/// <summary>
/// Scriptable CLI interface for GitDesktop.
/// Supports: status, log, branch, remote, fetch, pull, push, commit, stash, worktree, submodule, config, bisect, blame, grep.
/// Usage: gitdesktop-cli <command> [options] [repository-path]
/// </summary>
internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        var repoPath = args.Length > 1 ? args[^1] : Directory.GetCurrentDirectory();
        if (!Directory.Exists(repoPath)) repoPath = Directory.GetCurrentDirectory();

        var client = new GitDesktopClient();

        try
        {
            return command switch
            {
                "status" => await RunStatusAsync(client, repoPath),
                "log" => await RunLogAsync(client, repoPath, args),
                "branch" => await RunBranchAsync(client, repoPath, args),
                "remote" => await RunRemoteAsync(client, repoPath),
                "fetch" => await RunFetchAsync(client, repoPath, args),
                "pull" => await RunPullAsync(client, repoPath, args),
                "push" => await RunPushAsync(client, repoPath, args),
                "commit" => await RunCommitAsync(client, repoPath, args),
                "stash" => await RunStashAsync(client, repoPath, args),
                "worktree" => await RunWorktreeAsync(client, repoPath, args),
                "submodule" => await RunSubmoduleAsync(client, repoPath, args),
                "config" => await RunConfigAsync(client, repoPath, args),
                "blame" => await RunBlameAsync(client, repoPath, args),
                "grep" => await RunGrepAsync(client, repoPath, args),
                "reflog" => await RunReflogAsync(client, repoPath),
                "bisect" => await RunBisectAsync(client, repoPath, args),
                "help" or "--help" or "-h" => RunHelp(),
                _ => RunUnknown(command),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    static async Task<int> RunStatusAsync(GitDesktopClient client, string repoPath)
    {
        var status = await client.Repository.GetStatusAsync(repoPath);
        Console.WriteLine($"On branch {status.CurrentBranch}");
        if (status.UpstreamBranch != null)
            Console.WriteLine($"Your branch is {(status.AheadCount > 0 ? $"ahead of '{status.UpstreamBranch}' by {status.AheadCount} commit(s)" : $"up to date with '{status.UpstreamBranch}'")}");

        foreach (var entry in status.Entries)
            Console.WriteLine($"\t{entry.IndexStatus}/{entry.WorkTreeStatus}: {entry.Path}");

        if (!status.Entries.Any()) Console.WriteLine("nothing to commit, working tree clean");
        return 0;
    }

    static async Task<int> RunLogAsync(GitDesktopClient client, string repoPath, string[] args)
    {
        var limit = 20;
        if (args.Length > 1 && args[1].StartsWith("-n") && int.TryParse(args[1][2..], out var n)) limit = n;
        var commits = await client.History.GetLogAsync(repoPath, limit: limit);
        foreach (var c in commits)
            Console.WriteLine($"{c.ShortHash} {c.AuthorDate:yyyy-MM-dd} {c.AuthorName,-20} {c.Subject}");
        return 0;
    }

    static async Task<int> RunBranchAsync(GitDesktopClient client, string repoPath, string[] args)
    {
        var branches = await client.Branch.ListBranchesAsync(repoPath, includeRemotes: true);
        foreach (var b in branches)
            Console.WriteLine($"  {(b.IsCurrentBranch ? "*" : " ")} {b.Name,-40} {b.TipHash}");
        return 0;
    }

    static async Task<int> RunRemoteAsync(GitDesktopClient client, string repoPath)
    {
        var remotes = await client.Remote.ListRemotesAsync(repoPath);
        foreach (var r in remotes)
            Console.WriteLine($"  {r.Name,-20} {r.FetchUrl}");
        return 0;
    }

    static async Task<int> RunFetchAsync(GitDesktopClient client, string repoPath, string[] args)
    {
        var remote = args.Length > 1 ? args[1] : null;
        var result = await client.Remote.FetchAsync(repoPath, remote, prune: args.Contains("--prune"));
        Console.Write(result.Success ? result.Output : result.Error);
        return result.Success ? 0 : 1;
    }

    static async Task<int> RunPullAsync(GitDesktopClient client, string repoPath, string[] args)
    {
        var result = await client.Remote.PullAsync(repoPath);
        Console.Write(result.Success ? result.Output : result.Error);
        return result.Success ? 0 : 1;
    }

    static async Task<int> RunPushAsync(GitDesktopClient client, string repoPath, string[] args)
    {
        var result = await client.Remote.PushAsync(repoPath, force: args.Contains("--force"), forceWithLease: args.Contains("--force-with-lease"));
        Console.Write(result.Success ? result.Output : result.Error);
        return result.Success ? 0 : 1;
    }

    static async Task<int> RunCommitAsync(GitDesktopClient client, string repoPath, string[] args)
    {
        string? message = null;
        for (int i = 1; i < args.Length - 1; i++)
        {
            if (args[i] == "-m" && i + 1 < args.Length) { message = args[i + 1]; break; }
        }
        if (message == null) { Console.Error.WriteLine("Usage: commit -m <message>"); return 1; }
        var result = await client.Commit.CommitAsync(repoPath, message);
        Console.Write(result.Success ? result.Output : result.Error);
        return result.Success ? 0 : 1;
    }

    static async Task<int> RunStashAsync(GitDesktopClient client, string repoPath, string[] args)
    {
        var subcommand = args.Length > 1 ? args[1] : "list";
        if (subcommand == "list")
        {
            var stashes = await client.Commit.StashListAsync(repoPath);
            foreach (var s in stashes) Console.WriteLine(s);
        }
        else if (subcommand == "push")
        {
            var result = await client.Commit.StashPushAsync(repoPath);
            Console.Write(result.Success ? result.Output : result.Error);
            return result.Success ? 0 : 1;
        }
        else if (subcommand == "pop")
        {
            var result = await client.Commit.StashPopAsync(repoPath);
            Console.Write(result.Success ? result.Output : result.Error);
            return result.Success ? 0 : 1;
        }
        return 0;
    }

    static async Task<int> RunWorktreeAsync(GitDesktopClient client, string repoPath, string[] args)
    {
        var worktrees = await client.WorkTreeSubmodule.ListWorkTreesAsync(repoPath);
        foreach (var w in worktrees)
            Console.WriteLine($"  {(w.IsMainWorktree ? "[main]" : "      ")} {w.Path,-50} {w.Branch ?? "(detached)"}");
        return 0;
    }

    static async Task<int> RunSubmoduleAsync(GitDesktopClient client, string repoPath, string[] args)
    {
        var submodules = await client.WorkTreeSubmodule.ListSubmodulesAsync(repoPath);
        foreach (var s in submodules)
            Console.WriteLine($"  {s.Status,-15} {s.Path,-30} {s.CommitHash}");
        return 0;
    }

    static async Task<int> RunConfigAsync(GitDesktopClient client, string repoPath, string[] args)
    {
        var entries = await client.Config.ListAsync(repoPath);
        foreach (var e in entries)
            Console.WriteLine($"  {e.Key}={e.Value}");
        return 0;
    }

    static async Task<int> RunBlameAsync(GitDesktopClient client, string repoPath, string[] args)
    {
        var filePath = args.Length > 1 ? args[1] : null;
        if (filePath == null) { Console.Error.WriteLine("Usage: blame <file>"); return 1; }
        var blame = await client.History.BlameAsync(repoPath, filePath);
        foreach (var line in blame.Lines)
            Console.WriteLine($"{line.CommitHash[..8]} ({line.AuthorName,-20} {line.AuthorDate:yyyy-MM-dd}) {line.LineNumber,4}: {line.Content}");
        return 0;
    }

    static async Task<int> RunGrepAsync(GitDesktopClient client, string repoPath, string[] args)
    {
        var pattern = args.Length > 1 ? args[1] : null;
        if (pattern == null) { Console.Error.WriteLine("Usage: grep <pattern>"); return 1; }
        var result = await client.History.GrepAsync(repoPath, pattern);
        foreach (var match in result.Matches)
            Console.WriteLine($"{match.FilePath}:{match.LineNumber}: {match.LineContent}");
        return 0;
    }

    static async Task<int> RunReflogAsync(GitDesktopClient client, string repoPath)
    {
        var entries = await client.History.GetReflogAsync(repoPath, limit: 20);
        foreach (var e in entries)
            Console.WriteLine($"{e.Hash[..8]} {e.Ref,-30} {e.Message}");
        return 0;
    }

    static async Task<int> RunBisectAsync(GitDesktopClient client, string repoPath, string[] args)
    {
        var subcommand = args.Length > 1 ? args[1] : "log";
        var result = subcommand switch
        {
            "start" => await client.Bisect.StartAsync(repoPath),
            "good" => await client.Bisect.MarkGoodAsync(repoPath, args.Length > 2 ? args[2] : null),
            "bad" => await client.Bisect.MarkBadAsync(repoPath, args.Length > 2 ? args[2] : null),
            "skip" => await client.Bisect.SkipAsync(repoPath),
            "reset" => await client.Bisect.ResetAsync(repoPath),
            _ => await client.Bisect.GetLogAsync(repoPath),
        };
        Console.Write(result.Success ? result.Output : result.Error);
        return result.Success ? 0 : 1;
    }

    static void PrintUsage()
    {
        Console.WriteLine("""
            GitDesktop CLI - Scriptable Git interface

            Usage: gitdesktop-cli <command> [options] [repo-path]

            Commands:
              status              Show working tree status
              log [-n<N>]         Show commit log
              branch              List branches
              remote              List remotes
              fetch [remote]      Fetch from remote(s)
              pull                Pull from upstream
              push [--force]      Push to remote
              commit -m <msg>     Create a commit
              stash [list|push|pop] Manage stashes
              worktree            List worktrees
              submodule           List submodules
              config              List configuration
              blame <file>        Show blame for file
              grep <pattern>      Search file contents
              reflog              Show reflog
              bisect <sub>        Run bisect session
              help                Show this help
            """);
    }

    static int RunHelp() { PrintUsage(); return 0; }

    static int RunUnknown(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintUsage();
        return 1;
    }
}
