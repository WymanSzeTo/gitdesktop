using GitDesktop.Core;
using GitDesktop.Core.Execution;

namespace GitDesktop.App;

/// <summary>
/// Entry point for the GitDesktop application.
/// In a full implementation this would launch the Avalonia UI.
/// Currently provides a console-based interface for demonstration.
/// </summary>
internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("GitDesktop - Cross-platform Git Client (.NET 10)");
        Console.WriteLine("=================================================");

        var repoPath = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();

        var client = new GitDesktopClient();

        var repo = await client.Repository.OpenAsync(repoPath);
        if (repo == null)
        {
            Console.Error.WriteLine($"Not a git repository: {repoPath}");
            return 1;
        }

        Console.WriteLine($"Repository: {repo.Path}");
        Console.WriteLine($"Bare: {repo.IsBare}");
        Console.WriteLine($"Default branch: {repo.DefaultBranch}");
        Console.WriteLine($"Git: {repo.GitVersion}");
        Console.WriteLine();

        var status = await client.Repository.GetStatusAsync(repoPath);
        Console.WriteLine($"Branch: {status.CurrentBranch}");
        if (status.UpstreamBranch != null)
            Console.WriteLine($"Upstream: {status.UpstreamBranch} (ahead: {status.AheadCount}, behind: {status.BehindCount})");

        var entriesList = status.Entries.ToList();
        if (entriesList.Count == 0)
        {
            Console.WriteLine("Nothing to commit, working tree clean.");
        }
        else
        {
            Console.WriteLine($"\nChanges ({entriesList.Count}):");
            foreach (var entry in entriesList.Take(20))
                Console.WriteLine($"  {entry.WorkTreeStatus,-12} {entry.Path}");
        }

        Console.WriteLine();
        var branches = await client.Branch.ListBranchesAsync(repoPath);
        Console.WriteLine($"Branches ({branches.Count}):");
        foreach (var b in branches.Take(10))
            Console.WriteLine($"  {(b.IsCurrentBranch ? "*" : " ")} {b.Name}");

        return 0;
    }
}
