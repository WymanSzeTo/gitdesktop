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
│   │   └── Program.cs           (console + future Avalonia UI entry-point)
│   └── GitDesktop.Cli/
│       └── Program.cs           (scriptable CLI dispatcher)
├── tests/
│   └── GitDesktop.Core.Tests/   (xUnit tests using MockGitExecutor)
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

**`GitDesktop.App`** — currently a console demonstration.  The final implementation will host an
Avalonia UI that binds to `GitDesktopClient` via MVVM view-models.  The project has no coupling
to `GitDesktop.Cli`.

**`GitDesktop.Cli`** — a command dispatcher that maps CLI arguments to `GitDesktopClient` calls
and formats the output for terminal consumption.

---

## 4. Data Flow

### Typical Read Operation

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
              model returned to CLI / App
                          │
          CLI formats model as terminal output
```

### Typical Write Operation

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
| UI framework | Create a new project referencing `GitDesktop.Core`; build view-models on top of `GitDesktopClient` |

---

## 7. Target Platforms

| Platform | Status |
|----------|--------|
| Windows (x64, ARM64) | Supported |
| Linux (x64, ARM64) | Supported |
| macOS (x64, Apple Silicon) | Supported |

The `.NET 10` runtime and the system `git` binary are the only runtime requirements.
