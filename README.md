# gitdesktop
Use .Net 10 (major in C#) to develop Git Desktop Client covering all the functions of git.exe provided and running on Windows, Linux and MacOS.

## Features

- **Cross-platform GUI** — Avalonia UI desktop application that runs on Windows, Linux, and macOS.
- **CLI** — Scriptable `gitdesktop-cli` command-line interface for automation and CI pipelines.
- **Full Git coverage** — status, branches, history, commits, fetch/pull/push, stash, worktrees, submodules, bisect, LFS, hooks, and more.
- **MVVM architecture** — clean separation between ViewModels and Views for testability.
- **Multiple repository tabs** — open several repositories at the same time, each in its own tab; opening the same path twice switches to the existing tab instead of duplicating it.
- **Custom repository names** — supply an optional display name when opening a repository; rename any open tab later from the sidebar.
- **Saved repositories** — repositories are remembered across sessions via a JSON configuration file.
- **Session restore** — all tabs open when the app was last closed are re-opened automatically on the next launch, with the previously active tab pre-selected.
- **Diff highlighting** — staged/unstaged file diffs are shown with syntax-highlighted Add / Delete / Context lines in the Status view.
- **Language-aware code highlighting** — Files and History views detect source language by file extension and apply syntax-aware colouring.
- **Repository file list** — browse all tracked files in the active repository with an instant filter and detected-language badge.
- **5 colour themes** — Dark (default), Light, Monokai, Solarized Dark, and Nord.  Switch live from the toolbar.
- **Adjustable font size** — scale the UI font from the toolbar; the setting is persisted automatically.

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

The GUI opens a main window. Enter the path to any local git repository in the sidebar and click **Open** to load it.  Optionally enter a **Custom name** to give the tab a friendly label; if left blank the folder name is used.  Previously-opened repositories appear in the **Saved Repositories** list and can be reopened with a single click. Each repository opens in its own tab; opening the same repository path a second time switches to the existing tab.  On the next launch GitDesktop automatically re-opens all previously open tabs.

To **rename** an open tab, edit the value in the **TAB NAME** field at the bottom of the sidebar and click **Rename**; the new name is persisted to the configuration file.

#### GUI Views

| View | Features |
|------|----------|
| **Status** | Staged/unstaged/untracked files, stage/unstage, commit, amend, discard; select a file to view a colour-coded diff |
| **Files** | Filterable list of tracked files with language detection and syntax-aware content rendering |
| **Branches** | List local and remote branches, create, switch, delete, rename, merge |
| **History** | Commit log with syntax-coloured diff preview, cherry-pick, revert, reset |
| **Tags** | List tags, create lightweight and annotated tags, delete tags |
| **Remotes** | List remotes, add, remove remotes |
| **Stash** | List stashes with diff preview, push, apply, pop, drop stashes |

#### Settings (toolbar — top right)

| Setting | How to change |
|---------|---------------|
| **Theme** | Select from the drop-down: Dark / Light / Monokai / Solarized Dark / Nord |
| **Font size** | Drag the slider (9 – 24 px) |
| **Save** | Click **Save** to persist the current theme and font size to disk |

### Configuration file

Settings and known repositories are stored in:

| Platform | Path |
|----------|------|
| Windows | `%APPDATA%\GitDesktop\config.json` |
| Linux / macOS | `~/.config/GitDesktop/config.json` |

Example `config.json`:

```json
{
  "Repositories": [
    { "Name": "my-project",  "Path": "/home/user/my-project" },
    { "Name": "other-repo",  "Path": "/home/user/other-repo" }
  ],
  "Theme": "Monokai",
  "FontSize": 14,
  "OpenRepositoryPaths": ["/home/user/my-project", "/home/user/other-repo"],
  "SelectedRepositoryPath": "/home/user/my-project"
}
```

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
