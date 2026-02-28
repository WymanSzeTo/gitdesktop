# GitDesktop — Data Model

This document describes every model class in `GitDesktop.Core.Models` and how it maps to
concepts in the git object model.

---

## 1. Primitive Result

### `GitResult`  _(namespace: `GitDesktop.Core.Execution`)_

Returned by every method that wraps a git subprocess call.

| Property | Type | Description |
|----------|------|-------------|
| `ExitCode` | `int` | Raw process exit code |
| `Output` | `string` | Standard output (UTF-8, trailing whitespace trimmed) |
| `Error` | `string` | Standard error (UTF-8, trailing whitespace trimmed) |
| `Success` | `bool` | `true` when `ExitCode == 0` |

Factory helpers: `GitResult.Ok(output?)`, `GitResult.Fail(error, exitCode?)`.

---

## 2. Repository

### `Repository`

Snapshot of repository metadata returned by `RepositoryService.OpenAsync`.

| Property | Type | Description |
|----------|------|-------------|
| `Path` | `string` | Absolute path to the working tree root |
| `GitDirectory` | `string` | Absolute path to the `.git` directory |
| `IsBare` | `bool` | `true` for bare repositories |
| `DefaultBranch` | `string` | Name of the currently checked-out branch (e.g., `main`) |
| `GitVersion` | `string` | Version string from `git version` |

### `RepositoryStatus`

Result of `RepositoryService.GetStatusAsync` (parsed from `git status --porcelain=v2 --branch`).

| Property | Type | Description |
|----------|------|-------------|
| `CurrentBranch` | `string` | Active branch name; `(detached)` when in detached HEAD state |
| `UpstreamBranch` | `string?` | Tracking branch, if configured |
| `AheadCount` | `int` | Commits ahead of upstream |
| `BehindCount` | `int` | Commits behind upstream |
| `IsDetachedHead` | `bool` | `true` when HEAD is not on a branch |
| `Entries` | `IReadOnlyList<StatusEntry>` | File-level status entries |

Computed collections:

| Property | Filter |
|----------|--------|
| `StagedEntries` | Entries staged in the index |
| `UnstagedEntries` | Entries modified or deleted in the working tree |
| `UntrackedEntries` | New files not tracked by git |
| `ConflictedEntries` | Files in a merge conflict state |

---

## 3. Files & Status

### `StatusEntry`

One file entry from `git status`.

| Property | Type | Description |
|----------|------|-------------|
| `Path` | `string` | Relative path from repository root |
| `OriginalPath` | `string?` | Original path before rename/copy |
| `IndexStatus` | `FileStatusKind` | Status in the staging area (index) |
| `WorkTreeStatus` | `FileStatusKind` | Status in the working tree |
| `IsStaged` | `bool` | Computed: entry has index changes |
| `IsConflicted` | `bool` | Computed: entry has merge conflicts |

### `FileStatusKind` (enum)

| Value | Description |
|-------|-------------|
| `Untracked` | New file, not staged |
| `Modified` | File changed |
| `Added` | File staged as new |
| `Deleted` | File deleted |
| `Renamed` | File renamed |
| `Copied` | File copied |
| `Unmerged` | File has merge conflict |
| `Ignored` | File matches a `.gitignore` pattern |

---

## 4. Commits

### `Commit`

Represents a single git commit object.

| Property | Type | Description |
|----------|------|-------------|
| `Hash` | `string` | Full 40-character SHA-1 hash |
| `ShortHash` | `string` | Abbreviated hash |
| `Subject` | `string` | First line of the commit message |
| `Body` | `string` | Remainder of the commit message (after subject line) |
| `AuthorName` | `string` | Author display name |
| `AuthorEmail` | `string` | Author email address |
| `AuthorDate` | `DateTimeOffset` | When the author wrote the change |
| `CommitterName` | `string` | Committer display name |
| `CommitterEmail` | `string` | Committer email address |
| `CommitterDate` | `DateTimeOffset` | When the commit was recorded |
| `ParentHashes` | `IReadOnlyList<string>` | SHA-1 hashes of parent commits |
| `IsSigned` | `bool` | Whether the commit carries a GPG/SSH signature |
| `SignatureStatus` | `string` | Signature verification status string |

Computed properties:

| Property | Description |
|----------|-------------|
| `FullMessage` | `Subject + "\n\n" + Body` (or just `Subject` if body is empty) |
| `IsMergeCommit` | `true` when `ParentHashes.Count > 1` |

---

## 5. Branches & Tags

### `Branch`

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Short branch name (e.g., `main`, `origin/main`) |
| `FullName` | `string` | Full ref name (e.g., `remotes/origin/main`) |
| `Type` | `BranchType` | `Local` or `Remote` |
| `IsCurrentBranch` | `bool` | `true` for the active branch |
| `UpstreamName` | `string?` | Configured tracking branch |
| `AheadCount` | `int` | Commits ahead of upstream |
| `BehindCount` | `int` | Commits behind upstream |
| `TipHash` | `string` | Abbreviated SHA of the branch tip |

Computed: `IsLocal`, `IsRemote`, `HasUpstream`.

### `BranchType` (enum)
`Local`, `Remote`

### `Tag`

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Tag name |
| `Type` | `TagType` | `Lightweight` or `Annotated` |
| `TargetHash` | `string` | SHA of the tagged object |
| `Message` | `string?` | Tag annotation message (annotated tags only) |
| `TaggerName` | `string?` | Name of the person who created the tag |
| `TaggerEmail` | `string?` | Email of the tagger |
| `TaggerDate` | `DateTimeOffset?` | Date the tag was created |

Computed: `IsAnnotated`.

### `TagType` (enum)
`Lightweight`, `Annotated`

---

## 6. Remotes

### `Remote`

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Remote alias (e.g., `origin`) |
| `FetchUrl` | `string` | URL used for fetch operations |
| `PushUrl` | `string?` | URL used for push operations (may differ from fetch URL) |
| `FetchRefSpecs` | `IReadOnlyList<string>` | Configured fetch refspecs |
| `PushRefSpecs` | `IReadOnlyList<string>` | Configured push refspecs |

---

## 7. Diff

### `DiffLine`

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `DiffLineType` | Line classification |
| `Content` | `string` | Raw line content |
| `OldLineNumber` | `int?` | Line number in the old file |
| `NewLineNumber` | `int?` | Line number in the new file |

### `DiffLineType` (enum)
`Context`, `Added`, `Removed`, `Header`, `Hunk`

### `DiffHunk`

| Property | Type | Description |
|----------|------|-------------|
| `Header` | `string` | Raw hunk header (`@@ … @@`) |
| `OldStart` | `int` | Starting line in old file |
| `OldCount` | `int` | Number of lines in old file |
| `NewStart` | `int` | Starting line in new file |
| `NewCount` | `int` | Number of lines in new file |
| `Lines` | `IReadOnlyList<DiffLine>` | Lines in this hunk |

### `FileDiff`

| Property | Type | Description |
|----------|------|-------------|
| `OldPath` | `string` | Path before the change |
| `NewPath` | `string` | Path after the change |
| `Status` | `FileStatusKind` | Type of change |
| `IsBinary` | `bool` | `true` for binary files |
| `Hunks` | `IReadOnlyList<DiffHunk>` | List of diff hunks |

Computed: `DisplayPath` (prefers `NewPath`).

---

## 8. Stash

### `Stash`

| Property | Type | Description |
|----------|------|-------------|
| `Index` | `int` | Zero-based stash index |
| `Ref` | `string` | Ref name (e.g., `stash@{0}`) |
| `Message` | `string` | Stash description |
| `CommitHash` | `string` | SHA of the stash commit |
| `Date` | `DateTimeOffset` | When the stash was created |

---

## 9. Blame

### `BlameLine`

| Property | Type | Description |
|----------|------|-------------|
| `LineNumber` | `int` | 1-based line number |
| `Content` | `string` | Line text |
| `CommitHash` | `string` | SHA of the commit that last changed this line |
| `AuthorName` | `string` | Author display name |
| `AuthorEmail` | `string` | Author email |
| `AuthorDate` | `DateTimeOffset` | When the line was last changed |
| `Summary` | `string` | Commit subject line |

### `BlameResult`

| Property | Type | Description |
|----------|------|-------------|
| `FilePath` | `string` | Path of the blamed file |
| `Commit` | `string` | Commit or ref used for blame |
| `Lines` | `IReadOnlyList<BlameLine>` | Per-line blame annotations |

---

## 10. Reflog

### `ReflogEntry`

| Property | Type | Description |
|----------|------|-------------|
| `Index` | `int` | Zero-based position in the reflog |
| `Hash` | `string` | Commit SHA at this entry |
| `PreviousHash` | `string` | Previous SHA |
| `Ref` | `string` | Ref expression (e.g., `HEAD@{3}`) |
| `Action` | `string` | Action that created this entry (e.g., `commit`, `checkout`) |
| `Message` | `string` | Reflog message |
| `AuthorName` | `string` | Author of the action |
| `Date` | `DateTimeOffset` | Timestamp of the action |

---

## 11. Grep

### `GrepMatch`

| Property | Type | Description |
|----------|------|-------------|
| `FilePath` | `string` | Relative path of the matching file |
| `LineNumber` | `int` | 1-based line number |
| `LineContent` | `string` | Content of the matching line |
| `Ref` | `string` | Commit or tree ref searched |

### `GrepResult`

| Property | Type | Description |
|----------|------|-------------|
| `Pattern` | `string` | The search pattern used |
| `Matches` | `IReadOnlyList<GrepMatch>` | All matches across all files |

---

## 12. Worktrees

### `WorkTree`

| Property | Type | Description |
|----------|------|-------------|
| `Path` | `string` | Absolute path of the worktree |
| `Branch` | `string?` | Checked-out branch name (`null` when detached) |
| `HeadHash` | `string` | Current HEAD commit hash |
| `IsMainWorktree` | `bool` | `true` for the primary worktree |
| `IsBare` | `bool` | `true` for bare worktrees |
| `IsDetached` | `bool` | `true` when HEAD is detached |
| `Status` | `WorkTreeStatus` | Overall worktree health |
| `LockReason` | `string?` | Reason string when status is `Locked` |

### `WorkTreeStatus` (enum)
`Clean`, `Dirty`, `Locked`, `Prunable`

---

## 13. Submodules

### `Submodule`

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Submodule name (usually the path) |
| `Path` | `string` | Relative path within the parent repository |
| `Url` | `string` | Remote URL of the submodule |
| `Branch` | `string?` | Configured branch (may be `null`) |
| `CommitHash` | `string` | SHA recorded in the parent repository |
| `Status` | `SubmoduleStatus` | Current state of the submodule |

### `SubmoduleStatus` (enum)
`Uninitialized`, `UpToDate`, `Ahead`, `Behind`, `Modified`, `Conflict`

---

## 14. Configuration

### `ConfigEntry`

| Property | Type | Description |
|----------|------|-------------|
| `Key` | `string` | Configuration key (e.g., `user.email`) |
| `Value` | `string` | Configuration value |
| `Scope` | `ConfigScope` | Where the entry lives |

### `ConfigScope` (enum)
`System`, `Global`, `Local`

### `ConfigKeys` (static constants)

| Constant | Key |
|----------|-----|
| `UserName` | `user.name` |
| `UserEmail` | `user.email` |
| `UserSigningKey` | `user.signingkey` |
| `CommitGpgSign` | `commit.gpgsign` |
| `CoreAutoCrlf` | `core.autocrlf` |
| `CoreEditor` | `core.editor` |
| `PullRebase` | `pull.rebase` |
| `InitDefaultBranch` | `init.defaultBranch` |
| `RemoteOriginUrl` | `remote.origin.url` |
| `BranchAutoSetupMerge` | `branch.autoSetupMerge` |

---

## 15. Bisect

### `BisectState`

| Property | Type | Description |
|----------|------|-------------|
| `IsActive` | `bool` | `true` during an active bisect session |
| `GoodCommit` | `string?` | Last known-good commit |
| `BadCommit` | `string?` | Last known-bad commit |
| `CurrentCommit` | `string?` | Commit currently being tested |
| `RemainingSteps` | `int` | Estimated steps remaining |
| `GoodCommits` | `IReadOnlyList<string>` | All commits marked good |
| `BadCommits` | `IReadOnlyList<string>` | All commits marked bad |

---

## 16. Hooks

### `HookEntry`

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Hook name (e.g., `pre-commit`) |
| `FilePath` | `string` | Absolute path to the hook file |
| `IsEnabled` | `bool` | `true` when the file does not have a `.sample` suffix |
