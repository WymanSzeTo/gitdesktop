# GitDesktop — Quick Start Guide

## Prerequisites

| Requirement | Minimum Version | Notes |
|-------------|-----------------|-------|
| .NET SDK | 10.0 | <https://dotnet.microsoft.com/download> |
| Git | 2.38+ | Must be on `PATH` |

---

## 1. Get the Source

```bash
git clone https://github.com/WymanSzeTo/gitdesktop.git
cd gitdesktop
```

---

## 2. Build

```bash
dotnet build GitDesktop.slnx
```

A successful build produces:

* `src/GitDesktop.Core/` — the core library
* `src/GitDesktop.App/` — the desktop application entry-point
* `src/GitDesktop.Cli/` — the CLI tool

---

## 3. Run the CLI

```bash
# From the repository root
dotnet run --project src/GitDesktop.Cli -- status .
dotnet run --project src/GitDesktop.Cli -- log -n10 .
dotnet run --project src/GitDesktop.Cli -- branch .
dotnet run --project src/GitDesktop.Cli -- remote .
dotnet run --project src/GitDesktop.Cli -- help
```

Alternatively, publish as a self-contained executable:

```bash
dotnet publish src/GitDesktop.Cli -c Release -o ./out/cli
./out/cli/gitdesktop-cli status .
```

---

## 4. Run the Desktop App

```bash
dotnet run --project src/GitDesktop.App -- /path/to/repo
```

The application opens a full Avalonia GUI with six views — Status, Branches, History, Tags,
Remotes, and Stash — providing complete git functionality in a cross-platform desktop interface.

---

## 5. Use the Core Library in Your Project

Add a project reference:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/GitDesktop.Core/GitDesktop.Core.csproj" />
</ItemGroup>
```

### 5.1 Open a Repository

```csharp
using GitDesktop.Core;

var client = new GitDesktopClient();
var repo = await client.Repository.OpenAsync("/path/to/repo");
if (repo is null)
{
    Console.Error.WriteLine("Not a git repository.");
    return;
}
Console.WriteLine($"Branch: {repo.DefaultBranch}  Git: {repo.GitVersion}");
```

### 5.2 Check Status

```csharp
var status = await client.Repository.GetStatusAsync("/path/to/repo");
Console.WriteLine($"On branch {status.CurrentBranch}");
foreach (var entry in status.Entries)
    Console.WriteLine($"  {entry.WorkTreeStatus,-10} {entry.Path}");
```

### 5.3 Stage and Commit

```csharp
await client.Commit.StageAllAsync("/path/to/repo");
var result = await client.Commit.CommitAsync("/path/to/repo", "My commit message");
if (!result.Success)
    Console.Error.WriteLine(result.Error);
```

### 5.4 List Branches

```csharp
var branches = await client.Branch.ListBranchesAsync("/path/to/repo", includeRemotes: true);
foreach (var b in branches)
    Console.WriteLine($"{(b.IsCurrentBranch ? "* " : "  ")}{b.Name}");
```

### 5.5 Fetch and Pull

```csharp
await client.Remote.FetchAsync("/path/to/repo", prune: true);
await client.Remote.PullAsync("/path/to/repo");
```

### 5.6 View Commit Log

```csharp
var commits = await client.History.GetLogAsync("/path/to/repo", limit: 20);
foreach (var c in commits)
    Console.WriteLine($"{c.ShortHash}  {c.AuthorDate:yyyy-MM-dd}  {c.Subject}");
```

---

## 6. Run Tests

```bash
dotnet test
```

All tests use `MockGitExecutor` and do not require a real git installation or a live repository.

---

## 7. CLI Command Reference (Summary)

```
gitdesktop-cli <command> [options] [repo-path]

  status              Show working tree status
  log [-n<N>]         Show commit log (default: 20 entries)
  branch              List local and remote branches
  remote              List configured remotes
  fetch [remote]      Fetch from remote(s)  [--prune]
  pull                Pull from upstream
  push [--force]      Push to remote        [--force-with-lease]
  commit -m <msg>     Create a commit
  stash [list|push|pop]  Manage stash entries
  worktree            List worktrees
  submodule           List submodules
  config              List git configuration
  blame <file>        Show per-line blame annotation
  grep <pattern>      Search file contents
  reflog              Show reflog (last 20 entries)
  bisect <sub>        Run bisect session  (start|good|bad|skip|reset|log)
  help                Show this help text
```

---

## 8. Next Steps

* Read **[Architecture.md](Architecture.md)** for a deep-dive into the layered design.
* Read **[spec.md](spec.md)** for the full API specification.
* Read **[data-model.md](data-model.md)** for details of every model class.
* See **[TechnicalGuide.md](TechnicalGuide.md)** for contributor workflows.
* See **[InstallationGuide.md](InstallationGuide.md)** for packaging and Docker deployment.
