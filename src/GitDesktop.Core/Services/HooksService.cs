using GitDesktop.Core.Execution;

namespace GitDesktop.Core.Services;

/// <summary>
/// Service for hooks management.
/// </summary>
public sealed class HooksService
{
    private readonly IGitExecutor _git;

    public HooksService(IGitExecutor git)
    {
        _git = git;
    }

    /// <summary>List hooks in the repository.</summary>
    public IReadOnlyList<HookEntry> ListHooks(string repoPath)
    {
        var hooksDir = System.IO.Path.Combine(repoPath, ".git", "hooks");
        if (!System.IO.Directory.Exists(hooksDir)) return [];

        return System.IO.Directory.GetFiles(hooksDir)
            .Select(f =>
            {
                var name = System.IO.Path.GetFileName(f);
                var isSample = name.EndsWith(".sample");
                return new HookEntry
                {
                    Name = isSample ? name[..^".sample".Length] : name,
                    FilePath = f,
                    IsEnabled = !isSample,
                };
            })
            .ToList();
    }

    /// <summary>Enable a hook by removing its .sample extension.</summary>
    public bool EnableHook(string repoPath, string hookName)
    {
        var samplePath = System.IO.Path.Combine(repoPath, ".git", "hooks", $"{hookName}.sample");
        var activePath = System.IO.Path.Combine(repoPath, ".git", "hooks", hookName);
        if (!System.IO.File.Exists(samplePath)) return false;
        System.IO.File.Move(samplePath, activePath);
        return true;
    }

    /// <summary>Disable a hook by adding a .sample extension.</summary>
    public bool DisableHook(string repoPath, string hookName)
    {
        var activePath = System.IO.Path.Combine(repoPath, ".git", "hooks", hookName);
        var samplePath = $"{activePath}.sample";
        if (!System.IO.File.Exists(activePath)) return false;
        System.IO.File.Move(activePath, samplePath);
        return true;
    }

    /// <summary>Read hook script content.</summary>
    public string? ReadHook(string repoPath, string hookName)
    {
        var hookPath = System.IO.Path.Combine(repoPath, ".git", "hooks", hookName);
        return System.IO.File.Exists(hookPath) ? System.IO.File.ReadAllText(hookPath) : null;
    }

    /// <summary>Write hook script content.</summary>
    public void WriteHook(string repoPath, string hookName, string content)
    {
        var hookPath = System.IO.Path.Combine(repoPath, ".git", "hooks", hookName);
        System.IO.File.WriteAllText(hookPath, content);
        // Make executable on Unix
        if (!OperatingSystem.IsWindows())
        {
            System.IO.File.SetUnixFileMode(hookPath,
                System.IO.UnixFileMode.UserExecute | System.IO.UnixFileMode.UserRead | System.IO.UnixFileMode.UserWrite |
                System.IO.UnixFileMode.GroupRead | System.IO.UnixFileMode.GroupExecute |
                System.IO.UnixFileMode.OtherRead | System.IO.UnixFileMode.OtherExecute);
        }
    }
}

public sealed class HookEntry
{
    public string Name { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
}
