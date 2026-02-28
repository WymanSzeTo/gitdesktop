# GitDesktop — Project Constitution

## 1. Purpose

GitDesktop is a free, open-source, cross-platform Git client built on **.NET 10** and written
primarily in **C#**.  It exposes every capability of the `git` command-line tool through a
well-structured, testable library (`GitDesktop.Core`) and two consumer surfaces:

* **GitDesktop.App** — a graphical desktop application (Avalonia UI).
* **GitDesktop.Cli** — a scriptable command-line interface.

---

## 2. Guiding Principles

| # | Principle | Description |
|---|-----------|-------------|
| 1 | **Full git coverage** | Every operation available through `git.exe` / `git` MUST eventually be reachable via the public API of `GitDesktop.Core`. |
| 2 | **Cross-platform first** | The product MUST run without modification on Windows, Linux, and macOS. Platform-specific code is only permitted in narrowly scoped, clearly marked helpers. |
| 3 | **Thin wrapper, rich model** | `GitDesktop.Core` wraps the `git` subprocess.  It parses output into strongly-typed models; it does **not** re-implement git internals. |
| 4 | **Testability** | All services accept an `IGitExecutor` abstraction.  `MockGitExecutor` allows unit tests to verify command construction and output parsing without a real git installation. |
| 5 | **Minimal dependencies** | The core library has zero NuGet dependencies beyond the .NET BCL.  Additional dependencies must be justified and security-reviewed before introduction. |
| 6 | **Async by default** | All I/O-bound operations expose `async`/`await` APIs with `CancellationToken` support. Synchronous wrappers are not provided in the public API. |
| 7 | **Semantic versioning** | Public API changes follow [Semantic Versioning 2.0.0](https://semver.org/).  Breaking changes require a major-version bump. |
| 8 | **Inclusive community** | Contributors are expected to follow the project Code of Conduct (to be adopted by the maintainers). |

---

## 3. Project Governance

### 3.1 Roles

| Role | Responsibilities |
|------|-----------------|
| **Maintainer** | Merge pull requests, cut releases, manage the issue backlog, enforce the constitution. |
| **Contributor** | Submit issues and pull requests, participate in design discussions, write or improve tests. |
| **User** | File bug reports, request features, use the software. |

### 3.2 Decision Making

* Ordinary changes (bug fixes, test additions, documentation updates) require **one maintainer
  approval**.
* Changes to the public API of `GitDesktop.Core` or to this constitution require **discussion in
  a GitHub issue** before a pull request is opened.
* Breaking API changes must be called out explicitly in the pull-request description and in the
  release notes.

---

## 4. Versioning and Releases

* The project uses **git tags** (`vMAJOR.MINOR.PATCH`) to mark releases.
* Release notes are published alongside each tag describing new features, bug fixes, and breaking
  changes.
* The `main` branch is always in a releasable state.  Long-running feature work is done on feature
  branches and merged via pull request.

---

## 5. Quality Standards

* All new public methods MUST include XML documentation comments.
* Unit-test coverage for `GitDesktop.Core` is maintained using `MockGitExecutor`.
* The CI pipeline (GitHub Actions) must pass on all supported platforms before a pull request can
  be merged.
* Linting and static analysis warnings are treated as errors in CI.

---

## 6. Amendments

This constitution may be amended by opening a pull request with the proposed changes and reaching
consensus among the active maintainers.  The amendment history is preserved through git history.
