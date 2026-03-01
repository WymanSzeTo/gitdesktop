# GitDesktop — Functional & Technical Specification

## 1. Overview

GitDesktop is a **.NET 10** (C#) cross-platform Git client.  It consists of three projects:

| Project | Type | Responsibility |
|---------|------|----------------|
| `GitDesktop.Core` | Class library | All git business logic, models, and execution abstraction |
| `GitDesktop.App`  | Executable | Graphical desktop UI entry-point (Avalonia UI) |
| `GitDesktop.Cli`  | Executable | Scriptable command-line interface |

### 1.1 GUI Feature Summary

| Feature | Description |
|---------|-------------|
| **Multi-repository tabs** | Multiple repositories can be opened simultaneously, each in its own tab; opening the same path twice switches to the existing tab |
| **Saved repositories** | Known repository paths and names are persisted in JSON config and shown in the sidebar |
| **Custom repository name** | An optional "Custom name" field lets users assign a display name when opening a repository |
| **Tab rename** | The "TAB NAME" field in the sidebar allows renaming the currently open tab; the new name is persisted to config |
| **Session restore** | All open repository tabs (and the active selection) are re-opened automatically on the next launch |
| **Status & diff view** | Staged/unstaged/untracked files; selecting a file loads a syntax-highlighted diff |
| **File browser** | Filterable list of all tracked files via `git ls-files` |
| **5 colour themes** | Dark (default), Light, Monokai, Solarized Dark, Nord — switchable live |
| **Adjustable font size** | Slider in the toolbar; persisted in config |

---

## 2. Scope

The specification covers the `GitDesktop.Core` library — the single source of truth for all
git operations.  Both `GitDesktop.App` and `GitDesktop.Cli` are thin consumers of this library.

### 2.1 In Scope

* All porcelain and plumbing commands exposed by `git.exe`.
* Repository lifecycle: `init`, `clone`, `open`, `status`, `fsck`, `gc`, `count-objects`.
* Commit management: `add`, `reset`, `commit`, `amend`, `stash`, `clean`, `restore`.
* Branch and tag management: list, create, switch, checkout, rename, delete, upstream tracking.
* Remote management: add, remove, rename, set-url, fetch, pull, push.
* History and inspection: `log`, `show`, `diff`, `blame`, `reflog`, `grep`.
* Merge/rebase workflow: `merge`, `rebase`, `cherry-pick`, `revert`, `reset`.
* Worktrees: list, add, remove, prune.
* Submodules: list, add, init, update, sync, deinit.
* Configuration: get, set, unset, list (system / global / local scopes).
* Bisect: start, good, bad, skip, reset, run, log.
* Hooks: list, enable, disable, read, write.
* Git LFS: install, track, untrack, list-tracked, ls-files, fetch, prune, status.
* Advanced / plumbing: `cat-file`, `ls-tree`, `hash-object`, `update-ref`, `rev-parse`,
  `rev-list`, `describe`, `shortlog`, `pack-objects`, `update-index`, raw passthrough.

### 2.2 Out of Scope (v1)

* A built-in merge-conflict editor (the `MergeToolAsync` wrapper delegates to the user-configured
  external tool).
* Authentication management (SSH key generation, credential storage).
* Git server hosting features.

---

## 3. Execution Model

```
┌────────────────────┐
│   Consumer         │  GitDesktop.App  /  GitDesktop.Cli
└────────┬───────────┘
         │ calls
┌────────▼───────────┐
│  GitDesktopClient  │  aggregates all service instances
└────────┬───────────┘
         │ delegates to
┌────────▼───────────┐
│  *Service classes  │  RepositoryService, CommitService, …
└────────┬───────────┘
         │ invokes
┌────────▼───────────┐
│  IGitExecutor      │  interface (testable seam)
└────────┬───────────┘
    ┌────┴────┐
    │         │
┌───▼───┐ ┌──▼──────────────┐
│ Git   │ │ MockGitExecutor │
│Process│ │  (unit tests)   │
│Executor│ └─────────────────┘
└───────┘
```

### 3.1 `IGitExecutor`

```csharp
Task<GitResult> ExecuteAsync(string workingDirectory, string arguments, CancellationToken ct);
Task<GitResult> ExecuteWithInputAsync(string workingDirectory, string arguments, string input, CancellationToken ct);
```

### 3.2 `GitResult`

| Property | Type | Description |
|----------|------|-------------|
| `ExitCode` | `int` | Process exit code |
| `Output` | `string` | Standard output (UTF-8) |
| `Error` | `string` | Standard error (UTF-8) |
| `Success` | `bool` | `true` when `ExitCode == 0` |

---

## 4. Service API Summary

### 4.1 `RepositoryService`

| Method | git equivalent |
|--------|---------------|
| `InitAsync(path, bare?, initialBranch?)` | `git init` |
| `CloneAsync(url, dest, depth?, singleBranch?, branch?, recurseSubmodules?)` | `git clone` |
| `OpenAsync(path)` | `git rev-parse` |
| `GetStatusAsync(path)` | `git status --porcelain=v2 --branch` |
| `FsckAsync(path)` | `git fsck` |
| `GcAsync(path, aggressive?, auto?)` | `git gc` |
| `CountObjectsAsync(path, verbose?)` | `git count-objects` |
| `VerifyPackAsync(path, packFile)` | `git verify-pack` |

### 4.2 `CommitService`

| Method | git equivalent |
|--------|---------------|
| `StageAsync` | `git add -- <pathspec>` |
| `StageAllAsync` | `git add -A` |
| `UnstageAsync` | `git reset HEAD -- <pathspec>` |
| `StagePatchAsync` | `git add -p` |
| `CommitAsync(message, sign?, allowEmpty?)` | `git commit -m` |
| `AmendAsync(message?, noEdit?)` | `git commit --amend` |
| `StashPushAsync` / `StashListAsync` / `StashApplyAsync` / `StashPopAsync` / `StashDropAsync` / `StashShowAsync` | `git stash *` |
| `DiscardAsync` | `git checkout --` |
| `RestoreAsync(staged?, source?)` | `git restore` |
| `CleanDryRunAsync` / `CleanAsync` | `git clean` |

### 4.3 `BranchService`

| Method | git equivalent |
|--------|---------------|
| `ListBranchesAsync(includeRemotes?)` | `git branch -a` |
| `CreateAsync` | `git branch` |
| `SwitchAsync(createIfNotExists?)` | `git switch` |
| `CheckoutAsync(newBranch?)` | `git checkout` |
| `RenameAsync` | `git branch -m` |
| `DeleteAsync(force?)` | `git branch -d/-D` |
| `SetUpstreamAsync` / `UnsetUpstreamAsync` | `git branch --set-upstream-to / --unset-upstream` |
| `ListTagsAsync` | `git tag -l` |
| `CreateLightweightTagAsync` / `CreateAnnotatedTagAsync` | `git tag` |
| `DeleteTagAsync` / `DeleteRemoteTagAsync` | `git tag -d` / `git push --delete` |

### 4.4 `RemoteService`

| Method | git equivalent |
|--------|---------------|
| `ListRemotesAsync` | `git remote -v` |
| `AddAsync` / `RemoveAsync` / `RenameAsync` / `SetUrlAsync` | `git remote *` |
| `FetchAsync(remote?, prune?, all?)` | `git fetch` |
| `PullAsync(remote?, strategy?, autoStash?)` | `git pull` |
| `PushAsync(remote?, branch?, force?, forceWithLease?, setUpstream?, tags?)` | `git push` |
| `PushTagsAsync` | `git push --tags` |

### 4.5 `HistoryService`

| Method | git equivalent |
|--------|---------------|
| `GetLogAsync(branch?, author?, pathSpec?, limit, skip, since?, until?)` | `git log` |
| `GetCommitAsync(commitish)` | `git show -s` |
| `ShowAsync(commitish)` | `git show` |
| `DiffAsync(pathSpec?, cached?, ignoreWhitespace?)` | `git diff` |
| `DiffRefsAsync(from, to, pathSpec?)` | `git diff <from> <to>` |
| `BlameAsync(filePath, commit?)` | `git blame --porcelain` |
| `GetReflogAsync(ref?, limit)` | `git reflog show` |
| `GrepAsync(pattern, ref?, pathSpec?, useRegex?, ignoreCase?)` | `git grep` |

### 4.6 `MergeRebaseService`

| Method | git equivalent |
|--------|---------------|
| `MergeAsync` / `MergeAbortAsync` | `git merge` |
| `RebaseAsync` / `RebaseContinueAsync` / `RebaseSkipAsync` / `RebaseAbortAsync` | `git rebase` |
| `CherryPickAsync` / `CherryPickContinueAsync` / `CherryPickAbortAsync` | `git cherry-pick` |
| `RevertAsync` | `git revert` |
| `ResetAsync(target, mode)` | `git reset --soft/--mixed/--hard` |
| `MarkResolvedAsync` | `git add` |
| `MergeToolAsync` | `git mergetool` |
| `CheckoutConflictSideAsync(ours)` | `git checkout --ours/--theirs` |

### 4.7 `WorkTreeSubmoduleService`

| Method | git equivalent |
|--------|---------------|
| `ListWorkTreesAsync` | `git worktree list --porcelain` |
| `AddWorkTreeAsync` / `RemoveWorkTreeAsync` / `PruneWorkTreesAsync` | `git worktree *` |
| `ListSubmodulesAsync` | `git submodule status --recursive` |
| `AddSubmoduleAsync` / `InitSubmodulesAsync` / `UpdateSubmodulesAsync` / `SyncSubmodulesAsync` / `DeinitSubmoduleAsync` | `git submodule *` |

### 4.8 `ConfigService`

| Method | git equivalent |
|--------|---------------|
| `GetAsync(key, scope)` | `git config --get` |
| `SetAsync(key, value, scope)` | `git config` |
| `UnsetAsync(key, scope)` | `git config --unset` |
| `ListAsync(scope)` | `git config --list` |
| `EditAsync(scope)` | `git config --edit` |

### 4.9 `BisectService`

| Method | git equivalent |
|--------|---------------|
| `StartAsync` / `MarkGoodAsync` / `MarkBadAsync` / `SkipAsync` / `ResetAsync` / `RunAsync` / `GetLogAsync` | `git bisect *` |

### 4.10 `HooksService`

| Method | Description |
|--------|-------------|
| `ListHooks(repoPath)` | Enumerate `.git/hooks/` |
| `EnableHook` / `DisableHook` | Toggle `.sample` extension |
| `ReadHook` / `WriteHook` | Read / write hook script content |

### 4.11 `LfsService`

| Method | git equivalent |
|--------|---------------|
| `IsInstalledAsync` | `git lfs version` |
| `InstallAsync` | `git lfs install` |
| `TrackAsync` / `UntrackAsync` / `ListTrackedAsync` | `git lfs track/untrack` |
| `ListFilesAsync` | `git lfs ls-files` |
| `FetchAsync(all?)` | `git lfs fetch` |
| `PruneAsync` | `git lfs prune` |
| `StatusAsync` | `git lfs status` |

### 4.12 `AdvancedService`

| Method | git equivalent |
|--------|---------------|
| `CatFileAsync` | `git cat-file` |
| `LsTreeAsync(recursive?)` | `git ls-tree` |
| `HashObjectAsync(write?)` | `git hash-object` |
| `UpdateRefAsync` | `git update-ref` |
| `RevParseAsync` | `git rev-parse` |
| `RevListAsync(limit?)` | `git rev-list` |
| `DescribeAsync(tags?)` | `git describe` |
| `ShortlogAsync(summary?)` | `git shortlog` |
| `PackObjectsAsync` | `git pack-objects` |
| `UpdateIndexAsync` | `git update-index` |
| `RunRawAsync(arguments)` | raw `git <arguments>` |

---

## 5. CLI Commands

The `GitDesktop.Cli` executable supports:

```
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
help                Show help
```

---

## 6. Environment Variables

| Variable | Purpose |
|----------|---------|
| `GIT_TERMINAL_PROMPT` | Set to `0` by `GitProcessExecutor` to suppress credential prompts |

---

## 7. Error Handling

All service methods return `Task<GitResult>`.  Callers inspect `result.Success` (i.e., `ExitCode
== 0`) before consuming `result.Output`.  On failure, `result.Error` contains the stderr message.

Methods that return domain models (e.g., `OpenAsync`, `GetLogAsync`) return `null` or an empty
collection when the underlying git command fails, rather than throwing.

Exceptions are thrown only for:

* Unrecoverable execution errors (e.g., the git executable is not found).
* Programming errors (e.g., null argument where not permitted).
