# GitDesktop — Architecture

## 1. High-Level Overview

GitDesktop is a layered, cross-platform Git client.  The architecture separates concerns
into three distinct layers:

```
┌─────────────────────────────────────────────────────────────────┐
│                     Presentation Layer                          │
│  ┌──────────────────────────┐  ┌──────────────────────────┐    │
│  │     GitDesktop.App       │  │    GitDesktop.Cli         │    │
│  │  (Avalonia UI / Console) │  │  (Scriptable CLI)        │    │
│  └──────────────┬───────────┘  └──────────────┬───────────┘    │
└─────────────────┼──────────────────────────────┼───────────────┘
                  │                              │
                  └──────────┬───────────────────┘
                             │ references
┌────────────────────────────▼────────────────────────────────────┐
│                     Business Logic Layer                        │
│                      GitDesktop.Core                            │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │               GitDesktopClient                           │  │
│  │  ┌───────────┐ ┌───────────┐ ┌──────────┐ ┌──────────┐  │  │
│  │  │Repository │ │  Commit   │ │  Branch  │ │  Remote  │  │  │
│  │  │ Service   │ │  Service  │ │ Service  │ │ Service  │  │  │
│  │  └───────────┘ └───────────┘ └──────────┘ └──────────┘  │  │
│  │  ┌───────────┐ ┌───────────┐ ┌──────────┐ ┌──────────┐  │  │
│  │  │ History   │ │MergeRebase│ │WorkTree  │ │  Config  │  │  │
│  │  │ Service   │ │ Service   │ │Submodule │ │ Service  │  │  │
│  │  └───────────┘ └───────────┘ └──────────┘ └──────────┘  │  │
│  │  ┌───────────┐ ┌───────────┐ ┌──────────┐ ┌──────────┐  │  │
│  │  │  Bisect   │ │  Hooks    │ │   Lfs    │ │Advanced  │  │  │
│  │  │ Service   │ │ Service   │ │ Service  │ │ Service  │  │  │
│  │  └───────────┘ └───────────┘ └──────────┘ └──────────┘  │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                   Models namespace                       │  │
│  │  Repository  Commit  Branch  Tag  Remote  StatusEntry    │  │
│  │  Diff  Stash  Blame  Reflog  Grep  WorkTree  Submodule  │  │
│  │  ConfigEntry  BisectState  HookEntry                     │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                             │ delegates I/O through
┌────────────────────────────▼────────────────────────────────────┐
│                      Execution Layer                            │
│  ┌──────────────────────┐    ┌───────────────────────────────┐  │
│  │  IGitExecutor        │    │  GitResult                    │  │
│  │  (interface)         │    │  ExitCode / Output / Error    │  │
│  └──────────┬───────────┘    └───────────────────────────────┘  │
│    ┌────────┴──────────┐                                        │
│    │                   │                                        │
│  ┌─▼──────────────┐  ┌─▼────────────────┐                      │
│  │GitProcessExecutor│ │MockGitExecutor   │                      │
│  │(spawns git.exe) │ │(unit tests)      │                      │
│  └─────────────────┘ └──────────────────┘                      │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Project Structure

```
gitdesktop/
├── GitDesktop.slnx              Solution file
├── src/
│   ├── GitDesktop.Core/         Core library (no dependencies beyond BCL)
│   │   ├── Execution/
│   │   │   ├── IGitExecutor.cs
│   │   │   ├── GitResult.cs
│   │   │   ├── GitProcessExecutor.cs
│   │   │   └── MockGitExecutor.cs
│   │   ├── Models/
│   │   │   ├── Repository.cs    (Repository, RepositoryStatus)
│   │   │   ├── Commit.cs
│   │   │   ├── Branch.cs        (Branch, BranchType)
│   │   │   ├── Tag.cs           (Tag, TagType)
│   │   │   ├── Remote.cs
│   │   │   ├── StatusEntry.cs   (StatusEntry, RepositoryStatus, FileStatusKind)
│   │   │   ├── Diff.cs          (DiffLine, DiffHunk, FileDiff, DiffLineType)
│   │   │   ├── Stash.cs
│   │   │   ├── BlameResult.cs   (BlameLine, BlameResult)
│   │   │   ├── ReflogEntry.cs
│   │   │   ├── GrepResult.cs    (GrepMatch, GrepResult)
│   │   │   ├── WorkTree.cs      (WorkTree, WorkTreeStatus)
│   │   │   ├── Submodule.cs     (Submodule, SubmoduleStatus)
│   │   │   ├── ConfigEntry.cs   (ConfigEntry, ConfigScope, ConfigKeys)
│   │   │   └── BisectState.cs
│   │   ├── Services/
│   │   │   ├── RepositoryService.cs
│   │   │   ├── CommitService.cs
│   │   │   ├── BranchService.cs
│   │   │   ├── RemoteService.cs
│   │   │   ├── HistoryService.cs
│   │   │   ├── MergeRebaseService.cs
│   │   │   ├── WorkTreeSubmoduleService.cs
│   │   │   ├── ConfigService.cs
│   │   │   ├── BisectService.cs
│   │   │   ├── HooksService.cs
│   │   │   ├── LfsService.cs
│   │   │   └── AdvancedService.cs
│   │   └── GitDesktopClient.cs  (composition root for all services)
│   ├── GitDesktop.App/
│   │   ├── Program.cs           (Avalonia UI entry-point)
│   │   ├── App.axaml / App.axaml.cs   (theme resources + startup)
│   │   ├── Models/
│   │   │   └── AppConfig.cs           (AppConfig, RepositoryEntry)
│   │   ├── Converters/
│   │   │   └── ResourceKeyBrushConverter.cs (resource-key → IBrush)
│   │   ├── Services/
│   │   │   ├── AppConfigService.cs    (JSON config load/save)
│   │   │   └── ThemeManager.cs        (5 colour themes + apply)
│   │   ├── ViewModels/
│   │   │   ├── ViewModelBase.cs
│   │   │   ├── MainWindowViewModel.cs (tab management, settings)
│   │   │   ├── RepositoryTabViewModel.cs (per-repo tab state)
│   │   │   ├── StatusViewModel.cs     (+ diff loading)
│   │   │   ├── DiffLineViewModel.cs   (diff line + colour keys)
│   │   │   ├── FilesViewModel.cs      (repository file list)
│   │   │   ├── BranchesViewModel.cs
│   │   │   ├── HistoryViewModel.cs
│   │   │   ├── TagsViewModel.cs
│   │   │   ├── RemotesViewModel.cs
│   │   │   ├── StashViewModel.cs
│   │   │   ├── AsyncRelayCommand.cs
│   │   │   └── RelayCommand.cs        (+ RelayCommand<T>)
│   │   └── Views/
│   │       ├── MainWindow.axaml / MainWindow.axaml.cs
│   │       ├── StatusView.axaml / StatusView.axaml.cs  (+ diff panel)
│   │       ├── FilesView.axaml / FilesView.axaml.cs
│   │       ├── BranchesView.axaml / BranchesView.axaml.cs
│   │       ├── HistoryView.axaml / HistoryView.axaml.cs
│   │       ├── TagsView.axaml / TagsView.axaml.cs
│   │       ├── RemotesView.axaml / RemotesView.axaml.cs
│   │       └── StashView.axaml / StashView.axaml.cs
│   └── GitDesktop.Cli/
│       └── Program.cs           (scriptable CLI dispatcher)
├── tests/
│   ├── GitDesktop.Core.Tests/   (xUnit tests using MockGitExecutor)
│   └── GitDesktop.App.Tests/    (xUnit ViewModel tests)
└── docs/
    ├── constitution.md
    ├── spec.md
    ├── data-model.md
    ├── quickstart.md
    ├── Architecture.md          (this file)
    ├── TechnicalGuide.md
    └── InstallationGuide.md
```

---

## 3. Layer Descriptions

### 3.1 Execution Layer

The execution layer provides a thin, testable seam between the business logic and the operating
system process.

**`IGitExecutor`** — defines two async methods:

```csharp
Task<GitResult> ExecuteAsync(string workingDirectory, string arguments, CancellationToken ct);
Task<GitResult> ExecuteWithInputAsync(string workingDirectory, string arguments, string input, CancellationToken ct);
```

**`GitProcessExecutor`** — the production implementation.  It:

1. Constructs a `ProcessStartInfo` for `git` (or a custom executable path).
2. Sets `GIT_TERMINAL_PROMPT=0` to suppress credential dialogs.
3. Captures stdout and stderr via `OutputDataReceived` / `ErrorDataReceived` into `StringBuilder`
   instances.
4. Returns a `GitResult` after the process exits.

**`MockGitExecutor`** — the test double.  It allows tests to:

* Pre-register expected (`arguments` → `GitResult`) mappings.
* Verify that exact arguments were passed to the executor.

### 3.2 Business Logic Layer

All services follow a consistent pattern:

```csharp
public sealed class XxxService
{
    private readonly IGitExecutor _git;
    public XxxService(IGitExecutor git) { _git = git; }

    public Task<GitResult> DoSomethingAsync(string repoPath, ..., CancellationToken ct = default)
    {
        var args = BuildArguments(...);
        return _git.ExecuteAsync(repoPath, args, ct);
    }
}
```

Output-parsing logic is private to each service.  Parsed results are returned as strongly-typed
model objects rather than raw strings.

**`GitDesktopClient`** is the single composition root.  Consumers instantiate one client and
access all services through its properties:

```csharp
var client = new GitDesktopClient();          // real git process
var client = new GitDesktopClient(mockExec);  // unit test
```

### 3.3 Model Layer

All models are immutable `sealed class` or `sealed record` types.  Properties are declared with
`init`-only setters, making them safe to use in multi-threaded contexts.  No domain logic lives
in model classes beyond computed read-only properties derived from existing data.

### 3.4 Presentation Layer

**`GitDesktop.App`** — the cross-platform Avalonia UI desktop application.  It follows the MVVM
pattern:

* **`App`** — Avalonia application class; applies the Fluent theme, sets default DynamicResource
  colour and font-size entries, and loads the stored theme from `AppConfigService` on startup.
* **`AppConfigService`** — loads and saves `AppConfig` as JSON to
  `%APPDATA%\GitDesktop\config.json` (Windows) or `~/.config/GitDesktop/config.json`
  (Linux / macOS). Stores the list of known repositories, the active theme name, and font size.
* **`AppConfig` / `RepositoryEntry`** — the config model classes.
* **`ThemeManager`** — defines five colour themes (Dark, Light, Monokai, Solarized Dark, Nord)
  and applies the chosen theme by updating `Application.Current.Resources` at runtime.
* **`MainWindow`** — the root window.  Hosts a navigation sidebar, a top toolbar (Fetch / Pull /
  Push + theme/font-size settings), a status bar, and a `TabControl` for multiple repositories.
* **`MainWindowViewModel`** — top-level ViewModel.  Manages the collection of repository tabs
  (`ObservableCollection<RepositoryTabViewModel>`), opens new tabs, closes tabs, and exposes
  theme and font-size settings. Pass-through properties (`StatusVM`, `BranchesVM`, etc.) delegate
  to the selected tab for backward compatibility.
* **`RepositoryTabViewModel`** — holds all per-repository state: child ViewModels (`StatusVM`,
  `BranchesVM`, `HistoryVM`, `TagsVM`, `RemotesVM`, `StashVM`, `FilesVM`), the current view,
  and the Git operation commands (Fetch, Pull, Push).
* **`StatusViewModel`** — shows staged, unstaged, and untracked files.  When a file is selected
  the diff is loaded via `HistoryService.DiffAsync` and exposed as a list of
  `DiffLineViewModel` instances.
* **`DiffLineViewModel`** — wraps a `DiffLine` and exposes `BackgroundKey` / `ForegroundKey`
  resource-key strings.  In XAML these are resolved to `IBrush` via `ResourceKeyBrushConverter`.
* **`FilesViewModel`** — lists all tracked files via `git ls-files` and applies an in-memory
  filter.  Each content line is wrapped in a `FileLineViewModel` that carries a `ForegroundKey`
  string resolved in XAML by `ResourceKeyBrushConverter`.
* **`FileLineViewModel`** — represents a single source-file line with basic syntax classification
  (`Code`, `Comment`, `Keyword`, `String`) that drives the `ForegroundKey` resource name.
* **`MainWindowViewModel`** — manages open repository tabs (`Tabs`) and the saved-repositories
  sidebar list (`KnownRepositories`).  The sidebar list is backed by an
  `ObservableCollection<RepositoryEntry>` so that Avalonia's `ListBox` receives
  `INotifyCollectionChanged` notifications on add/remove/rename without stale rendering.
* **`BranchesViewModel`** — lists all local and remote branches.  Exposes commands to switch,
  create, delete, rename, and merge branches.
* **`HistoryViewModel`** — displays the commit log and shows the diff for the selected commit.
  Exposes commands to cherry-pick, revert, and reset to a commit.
* **`TagsViewModel`** — lists tags.  Exposes commands to create and delete tags.
* **`RemotesViewModel`** — lists configured remotes.  Exposes commands to add and remove remotes.
* **`StashViewModel`** — lists stashes with diff preview.  Exposes commands to push, apply,
  pop, and drop stashes.
* **`AsyncRelayCommand` / `RelayCommand` / `RelayCommand<T>`** — lightweight `ICommand`
  wrappers (no external MVVM framework dependency).

The view classes (`StatusView`, `FilesView`, `BranchesView`, `HistoryView`, `TagsView`,
`RemotesView`, `StashView`) are pure Avalonia `UserControl` XAML. Background and foreground
colours for diff and file-content lines are resolved at runtime through
`ResourceKeyBrushConverter`, which looks up `Application.Current.Resources` by the key string
exposed by the view-model.  Other colours and font sizes continue to use `DynamicResource`
directly, so they respond immediately when the user switches theme or adjusts font size.

**`GitDesktop.Cli`** — a command dispatcher that maps CLI arguments to `GitDesktopClient` calls
and formats the output for terminal consumption.

---

## 4. Data Flow

### GUI Read Operation (MVVM)

```
User navigates to view ──► ViewModel.RefreshAsync()
                                    │
                           service method ──► IGitExecutor.ExecuteAsync
                                                    │
                                git process spawned ◄─┘
                                stdout/stderr captured
                                        │
                            GitResult returned to service
                                        │
                            service parses output into model
                                        │
                            ViewModel updates ObservableCollection
                                        │
                            Avalonia binding updates View automatically
```

### GUI Write Operation (MVVM Command)

```
User triggers ICommand ──► ViewModel async method
                                    │
                    service method builds args
                                    │
                    IGitExecutor.ExecuteAsync
                                    │
                        git process modifies repo
                                    │
                    GitResult returned to ViewModel
                                    │
                ViewModel sets StatusMessage / refreshes
```

### CLI Read Operation

```
CLI command ──► service method ──► IGitExecutor.ExecuteAsync
                                           │
                    git process spawned ◄─┘
                    stdout/stderr captured
                          │
              GitResult returned to service
                          │
              service parses output into model
                          │
              model returned to CLI
                          │
          CLI formats model as terminal output
```

### CLI Write Operation

```
CLI command ──► service method builds argument string
               ──► IGitExecutor.ExecuteAsync
                           │
                   git process modifies repo
                           │
               GitResult(ExitCode, Output, Error) returned
                           │
               CLI checks result.Success, prints output/error
```
---

## 5. Threading Model

* All I/O methods are `async Task<T>`.  Services do not block threads.
* `CancellationToken` is propagated to `Process.WaitForExitAsync`.
* Services are stateless and thread-safe by construction — they hold only the executor reference.
* `GitDesktopClient` itself is also stateless and can be shared across threads.

---

## 6. Extension Points

| Point | How to Extend |
|-------|---------------|
| Custom git executable | Pass the path to `new GitProcessExecutor("path/to/git")` |
| Command interception / logging | Implement `IGitExecutor`, wrap `GitProcessExecutor`, inject the wrapper |
| New git commands | Add a method to an existing `*Service` or create a new service and expose it on `GitDesktopClient` |
| New GUI views | Add a ViewModel extending `ViewModelBase`, create an Avalonia `UserControl`, and register a `DataTemplate` in `MainWindow.axaml` |

---

## 7. Target Platforms

| Platform | GUI (`GitDesktop.App`) | CLI (`GitDesktop.Cli`) |
|----------|:----------------------:|:----------------------:|
| Windows (x64, ARM64) | ✅ Supported | ✅ Supported |
| Linux (x64, ARM64) | ✅ Supported | ✅ Supported |
| macOS (x64, Apple Silicon) | ✅ Supported | ✅ Supported |

The `.NET 10` runtime and the system `git` binary are the only runtime requirements.
The GUI requires a display server (X11 or Wayland on Linux; native on Windows and macOS).
