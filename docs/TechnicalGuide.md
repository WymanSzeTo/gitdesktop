# GitDesktop — Technical Guide

This guide is for contributors and maintainers who want to understand the internals, add new
features, or maintain the `GitDesktop.Core` library.

---

## 1. Development Environment

### 1.1 Required Tools

| Tool | Version | Purpose |
|------|---------|---------|
| .NET SDK | 10.0+ | Build, test, publish |
| Git | 2.38+ | Source control (and runtime dependency) |
| Any IDE / editor | — | VS 2022, VS Code + C# Dev Kit, Rider |

### 1.2 Clone and Verify

```bash
git clone https://github.com/WymanSzeTo/gitdesktop.git
cd gitdesktop
dotnet build GitDesktop.slnx
dotnet test
```

All tests should pass on a clean clone.

---

## 2. Repository Layout

```
gitdesktop/
├── GitDesktop.slnx              Solution file (.NET 10 slnx format)
├── src/
│   ├── GitDesktop.Core/         Core library (no external NuGet deps)
│   │   ├── Execution/           IGitExecutor, GitResult, executors
│   │   ├── Models/              Immutable domain models
│   │   └── Services/            One service class per git domain area
│   ├── GitDesktop.App/          App entry-point
│   └── GitDesktop.Cli/          CLI dispatcher
├── tests/
│   └── GitDesktop.Core.Tests/   Unit tests (MockGitExecutor)
└── docs/                        Documentation (this file)
```

---

## 3. Core Design Patterns

### 3.1 Dependency Injection via `IGitExecutor`

Every service receives its git executor through constructor injection:

```csharp
public sealed class RepositoryService
{
    private readonly IGitExecutor _git;
    public RepositoryService(IGitExecutor git) => _git = git;
}
```

This makes every service unit-testable without a real git installation.

### 3.2 Argument Construction

Service methods build git argument strings inline using C# string interpolation.  Special
characters in user-supplied strings (file paths, branch names, messages) are enclosed in double
quotes or escaped:

```csharp
var args = $"commit -m \"{EscapeMessage(message)}\"";
```

The `EscapeMessage` helper replaces `"` with `\"` and newlines with `\n`.

### 3.3 Output Parsing

Parsing raw git output into domain models is a private responsibility of each service.  Parser
methods are `private static` and named `Parse*`:

```csharp
private static RepositoryStatus ParseStatus(string output) { … }
private static Commit? ParseCommit(string record, string sep) { … }
```

Use controlled separators (e.g., `--format` / `--pretty` tokens) to produce machine-readable
output that is straightforward to split.

### 3.4 Return Conventions

| Return type | When to use |
|-------------|-------------|
| `Task<GitResult>` | Pure write/execute commands where only success/failure matters |
| `Task<TModel?>` | Commands that return a single model object; `null` on failure |
| `Task<IReadOnlyList<TModel>>` | Commands that return collections; empty list on failure |
| `Task<bool>` | Commands that need only a boolean answer |

---

## 4. Adding a New Command

### Step 1 — Identify the service

Find the most appropriate existing service class, or create a new one if the command belongs to
a new domain area.

### Step 2 — Add the method

```csharp
/// <summary>Runs git <command>.</summary>
public Task<GitResult> NewCommandAsync(string repoPath, string param, CancellationToken ct = default)
{
    var args = $"<command> \"{param}\"";
    return _git.ExecuteAsync(repoPath, args, ct);
}
```

Follow the conventions in §3.

### Step 3 — Expose via `GitDesktopClient`

If you created a new service, add a property to `GitDesktopClient` and wire it up in the
constructor:

```csharp
public NewService NewFeature { get; }

public GitDesktopClient(IGitExecutor? executor = null)
{
    …
    NewFeature = new NewService(Executor);
}
```

### Step 4 — Write a unit test

Use `MockGitExecutor` to verify:

1. The correct argument string is passed to the executor.
2. The output is correctly parsed into the expected model.

```csharp
[Fact]
public async Task NewCommandAsync_PassesCorrectArguments()
{
    var mock = new MockGitExecutor();
    mock.Register("new-command \"value\"", GitResult.Ok("expected output"));
    var service = new NewService(mock);
    var result = await service.NewCommandAsync("/repo", "value");
    Assert.True(result.Success);
    Assert.Equal("expected output", result.Output);
}
```

### Step 5 — Expose in CLI (optional)

Add a `case` to the `Main` dispatcher in `GitDesktop.Cli/Program.cs` and a corresponding
`RunXxxAsync` helper method.

---

## 5. Testing Strategy

### 5.1 Unit Tests (`GitDesktop.Core.Tests`)

All tests in `GitDesktop.Core.Tests` use `MockGitExecutor` and run completely offline.

**Test naming convention:**

```
<MethodUnderTest>_<Condition>_<ExpectedBehaviour>
```

Examples:
* `InitAsync_BareAndBranch_BuildsCorrectArgs`
* `ListBranchesAsync_IncludeRemotes_ReturnsMixedBranches`
* `ParseStatus_WithAheadBehind_SetsCountsCorrectly`

**Common assertion patterns:**

```csharp
// Verify argument construction
Assert.Equal("expected args string", mock.LastArguments);

// Verify model parsing
Assert.Equal("main", status.CurrentBranch);
Assert.Equal(3, status.AheadCount);

// Verify failure handling
Assert.True(result.IsEmpty()); // or Assert.Null(result)
```

### 5.2 Running Tests

```bash
dotnet test                     # all tests
dotnet test --filter "ClassName=BranchServiceTests"
dotnet test --logger "console;verbosity=detailed"
```

---

## 6. Coding Standards

### 6.1 Language Features

* **Nullable reference types** are enabled (`<Nullable>enable</Nullable>`).  Mark all nullable
  return types and parameters explicitly.
* **Implicit usings** are enabled.  Avoid redundant `using` directives.
* Prefer `sealed` classes for all new model and service types.
* Use C# 12+ primary constructors where it improves readability.

### 6.2 Documentation Comments

Every `public` type, property, and method must have an XML doc comment:

```csharp
/// <summary>Clones a repository.</summary>
/// <param name="url">Source repository URL.</param>
/// <param name="destination">Local directory for the clone.</param>
public async Task<GitResult> CloneAsync(string url, string destination, …)
```

### 6.3 Formatting

* 4-space indentation (no tabs).
* Opening braces on the same line for methods and control blocks.
* `var` for local variables where the type is obvious from context.
* Expression-bodied members for trivial single-expression methods.

### 6.4 Async

* Always use `async`/`await` rather than blocking with `.Result` or `.Wait()`.
* Accept `CancellationToken ct = default` as the last parameter on all async public methods.
* Propagate the token to all awaited calls, including `_git.ExecuteAsync`.

---

## 7. `GitProcessExecutor` Internals

`GitProcessExecutor` uses `System.Diagnostics.Process` to spawn the git executable:

```csharp
var psi = new ProcessStartInfo(_gitExecutable, arguments)
{
    WorkingDirectory = workingDirectory,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false,
    CreateNoWindow = true,
    StandardOutputEncoding = Encoding.UTF8,
    StandardErrorEncoding = Encoding.UTF8,
};
psi.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
```

Key points:

* `GIT_TERMINAL_PROMPT=0` prevents git from blocking on credential prompts in a headless
  environment.
* Output and error are read asynchronously via `OutputDataReceived` / `ErrorDataReceived` events.
* `ExecuteWithInputAsync` pipes a string to stdin and closes the stream before waiting for exit.
* Trailing whitespace is trimmed from `Output` and `Error` before constructing the `GitResult`.

---

## 8. `MockGitExecutor`

`MockGitExecutor` is the test double for `IGitExecutor`.  Usage:

```csharp
var mock = new MockGitExecutor();
// Register a fixed response for a specific argument string
mock.Register("status --porcelain=v2 --branch", GitResult.Ok(statusOutput));

var service = new RepositoryService(mock);
var status = await service.GetStatusAsync("/repo");
```

The mock matches by **exact argument string** (case-sensitive).  If no matching registration is
found it returns `GitResult.Fail("unregistered command")` or throws, depending on the
configuration.

---

## 9. Parsing Conventions

### 9.1 `--porcelain` and `--format` options

Always use machine-readable output formats when parsing:

| Command | Format used |
|---------|-------------|
| `git status` | `--porcelain=v2 --branch` |
| `git log` | `--pretty=format:…` with `\x1f` (unit separator) and `\x1e` (record separator) |
| `git branch` | `--format=%(refname:short)\|%(objectname:short)\|…` |
| `git worktree list` | `--porcelain` |
| `git stash list` | `--format=%gd\|%gs\|%H\|%ai` |
| `git blame` | `--porcelain` |
| `git reflog show` | `--format=%H\|%gd\|%gs\|%ai\|%an` |

### 9.2 Separator strategy for `git log`

The commit log parser uses two control characters as separators to avoid conflicts with user data:

* `\x1f` (ASCII Unit Separator) — separates fields within a single commit record.
* `\x1e` (ASCII Record Separator) — separates commit records from each other.

---

## 10. Release Process

1. Ensure `main` is green (all CI checks pass).
2. Update the version in each `.csproj` (if NuGet packaging is enabled).
3. Tag the release commit: `git tag v1.2.3 && git push origin v1.2.3`.
4. The CI pipeline builds, tests, and publishes release artifacts.
5. Publish release notes on the GitHub Releases page.

---

## 11. Contribution Workflow

1. Fork the repository.
2. Create a feature branch: `git switch -c feature/my-feature`.
3. Make changes, add/update tests, update relevant documentation.
4. Run `dotnet build && dotnet test` locally.
5. Open a pull request against `main`.
6. Address review feedback.
7. A maintainer merges the PR after approval.

---

## 12. Troubleshooting

| Problem | Solution |
|---------|----------|
| `git` not found | Ensure git is on `PATH`, or pass the full path to `new GitProcessExecutor("/usr/bin/git")` |
| Tests fail with "unregistered command" | The mock does not have a registration for the arguments your code is generating — check argument construction in the service method |
| `GIT_TERMINAL_PROMPT` warning | Expected on headless CI — this is intentional |
| Build error: `net10.0` not found | Install the .NET 10 SDK from <https://dotnet.microsoft.com/download> |
