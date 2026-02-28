# gitdesktop
Use .Net 10 (major in C#) to develop Git Desktop Client covering all the functions of git.exe provided and running on Windows, Linux and MacOS.

## Features

- **Cross-platform GUI** — Avalonia UI desktop application that runs on Windows, Linux, and macOS.
- **CLI** — Scriptable `gitdesktop-cli` command-line interface for automation and CI pipelines.
- **Full Git coverage** — status, branches, history, commits, fetch/pull/push, stash, worktrees, submodules, bisect, LFS, hooks, and more.
- **MVVM architecture** — clean separation between ViewModels and Views for testability.

## Projects

| Project | Type | Description |
|---------|------|-------------|
| `GitDesktop.Core` | Class library | All Git business logic, models, and execution abstraction |
| `GitDesktop.App`  | Avalonia UI executable | Cross-platform desktop GUI |
| `GitDesktop.Cli`  | Console executable | Scriptable CLI interface |

## Quick Start

### Run the GUI

```bash
dotnet run --project src/GitDesktop.App
```

The GUI opens a main window. Enter the path to any local git repository in the sidebar and click **Open** to load it. Use the **Status**, **Branches**, **History**, **Tags**, **Remotes**, and **Stash** views to inspect and interact with the repository.

#### GUI Views

| View | Features |
|------|----------|
| **Status** | Staged/unstaged/untracked files, stage/unstage, commit, amend, discard changes |
| **Branches** | List local and remote branches, create, switch, delete, rename, merge |
| **History** | Commit log with diff preview, cherry-pick, revert, reset |
| **Tags** | List tags, create lightweight and annotated tags, delete tags |
| **Remotes** | List remotes, add, remove remotes |
| **Stash** | List stashes with diff preview, push, apply, pop, drop stashes |

### Run the CLI

```bash
dotnet run --project src/GitDesktop.Cli -- status /path/to/repo
dotnet run --project src/GitDesktop.Cli -- log -n10 /path/to/repo
dotnet run --project src/GitDesktop.Cli -- help
```

## Build & Test

```bash
dotnet build GitDesktop.slnx
dotnet test
```

