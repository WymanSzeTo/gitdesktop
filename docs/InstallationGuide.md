# GitDesktop — Installation Guide

This guide covers two installation methods:

* **[Option A — Direct Installation](#option-a--direct-installation)** — build and run from source
  or install a pre-built binary on Windows, Linux, or macOS.
* **[Option B — Docker Installation](#option-b--docker-installation)** — run the CLI in a
  container without installing .NET locally.

---

## Prerequisites

| Requirement | Direct Install | Docker |
|-------------|:--------------:|:------:|
| Git 2.38+   | ✅ required     | ✅ required (on host for repo access) |
| .NET 10 SDK | ✅ required     | ❌ not needed on host |
| Docker Engine 24+ | ❌ optional | ✅ required |

---

## Option A — Direct Installation

### A1. Install .NET 10 SDK

Follow the official instructions for your platform:

* **Windows / macOS / Linux**: <https://dotnet.microsoft.com/download/dotnet/10.0>

Verify the installation:

```bash
dotnet --version
# Expected output: 10.x.x
```

### A2. Install Git

* **Windows**: <https://git-scm.com/download/win> (or via winget: `winget install Git.Git`)
* **macOS**: `brew install git`
* **Linux (Debian/Ubuntu)**: `sudo apt-get install git`
* **Linux (Fedora/RHEL)**: `sudo dnf install git`

Verify:

```bash
git --version
# Expected output: git version 2.x.x
```

### A3. Clone the Repository

```bash
git clone https://github.com/WymanSzeTo/gitdesktop.git
cd gitdesktop
```

### A4. Build

```bash
dotnet build GitDesktop.slnx -c Release
```

### A5. Run Tests (Optional but Recommended)

```bash
dotnet test
```

### A6. Publish a Self-Contained CLI Binary

A self-contained binary bundles the .NET runtime so the target machine does not need .NET
installed separately.

**Windows (x64):**

```powershell
dotnet publish src/GitDesktop.Cli `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o ./publish/win-x64
```

**Linux (x64):**

```bash
dotnet publish src/GitDesktop.Cli \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o ./publish/linux-x64
chmod +x ./publish/linux-x64/gitdesktop-cli
```

**macOS (Apple Silicon):**

```bash
dotnet publish src/GitDesktop.Cli \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  -o ./publish/osx-arm64
chmod +x ./publish/osx-arm64/gitdesktop-cli
```

### A7. (Optional) Add the Binary to PATH

**Linux / macOS:**

```bash
sudo cp ./publish/linux-x64/gitdesktop-cli /usr/local/bin/
gitdesktop-cli --help
```

**Windows (PowerShell — add to user PATH):**

```powershell
$dest = "$env:USERPROFILE\AppData\Local\GitDesktopCli"
New-Item -ItemType Directory -Force -Path $dest | Out-Null
Copy-Item .\publish\win-x64\gitdesktop-cli.exe $dest
[Environment]::SetEnvironmentVariable(
  "PATH", "$env:PATH;$dest",
  [System.EnvironmentVariableTarget]::User)
```

Open a new terminal and run:

```cmd
gitdesktop-cli --help
```

### A8. Run the Desktop GUI

Start the GUI directly from the source:

```bash
dotnet run --project src/GitDesktop.App
```

Enter the path to any local git repository in the sidebar and click **Open**.

### A9. Publish a Self-Contained GUI Binary

**Windows (x64):**

```powershell
dotnet publish src/GitDesktop.App `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o ./publish/app-win-x64
./publish/app-win-x64/GitDesktop.App.exe
```

**Linux (x64):**

```bash
dotnet publish src/GitDesktop.App \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o ./publish/app-linux-x64
chmod +x ./publish/app-linux-x64/GitDesktop.App
./publish/app-linux-x64/GitDesktop.App
```

> **Note (Linux):** The GUI requires a display server.  Set the `DISPLAY` environment variable
> if running in a remote or headless environment.

**macOS (Apple Silicon):**

```bash
dotnet publish src/GitDesktop.App \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  -o ./publish/app-osx-arm64
./publish/app-osx-arm64/GitDesktop.App
```

---

## Option B — Docker Installation

The Docker image packages the `gitdesktop-cli` binary so you can run it on any machine with
Docker Engine — no .NET SDK required on the host.

### B1. Build the Docker Image

Create a `Dockerfile` in the repository root (or use the one below):

```dockerfile
# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY GitDesktop.slnx ./
COPY src/GitDesktop.Core/GitDesktop.Core.csproj src/GitDesktop.Core/
COPY src/GitDesktop.Cli/GitDesktop.Cli.csproj   src/GitDesktop.Cli/
COPY src/GitDesktop.App/GitDesktop.App.csproj   src/GitDesktop.App/
RUN dotnet restore GitDesktop.slnx

# Copy the rest of the source and publish
COPY . .
RUN dotnet publish src/GitDesktop.Cli \
      -c Release \
      -r linux-x64 \
      --self-contained true \
      -o /app/publish

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM debian:bookworm-slim AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends git && rm -rf /var/lib/apt/lists/*
WORKDIR /repo
COPY --from=build /app/publish /usr/local/bin/
RUN chmod +x /usr/local/bin/gitdesktop-cli
ENTRYPOINT ["gitdesktop-cli"]
CMD ["help"]
```

Build the image:

```bash
docker build -t gitdesktop-cli:latest .
```

### B2. Run the CLI from Docker

Mount your local git repository into the container at `/repo` and pass CLI arguments:

```bash
# Show status of the current directory (Linux / macOS)
docker run --rm -v "$(pwd)":/repo gitdesktop-cli:latest status /repo

# Show the last 10 commits
docker run --rm -v "$(pwd)":/repo gitdesktop-cli:latest log -n10 /repo

# List branches
docker run --rm -v "$(pwd)":/repo gitdesktop-cli:latest branch /repo

# List remotes
docker run --rm -v "$(pwd)":/repo gitdesktop-cli:latest remote /repo
```

**Windows (PowerShell):**

```powershell
docker run --rm -v "${PWD}:/repo" gitdesktop-cli:latest status /repo
```

### B3. Use a Convenience Shell Alias (Linux / macOS)

```bash
alias gd='docker run --rm -v "$(pwd)":/repo gitdesktop-cli:latest'

gd status /repo
gd log -n10 /repo
gd branch /repo
gd help
```

### B4. Docker Compose (Optional)

If you want to integrate GitDesktop CLI into a larger toolchain:

```yaml
# docker-compose.yml
services:
  gitdesktop-cli:
    image: gitdesktop-cli:latest
    build:
      context: .
      dockerfile: Dockerfile
    volumes:
      - .:/repo
    working_dir: /repo
    entrypoint: gitdesktop-cli
    command: ["help"]
```

Run:

```bash
docker compose run --rm gitdesktop-cli status /repo
docker compose run --rm gitdesktop-cli log -n20 /repo
```

### B5. Pull a Pre-Built Image (When Published)

Once published to a container registry:

```bash
docker pull ghcr.io/wymanszeto/gitdesktop/gitdesktop-cli:latest
docker run --rm -v "$(pwd)":/repo ghcr.io/wymanszeto/gitdesktop/gitdesktop-cli:latest status /repo
```

---

## Verifying the Installation

Run the following to confirm everything is working:

```bash
gitdesktop-cli help
```

Expected output:

```
GitDesktop CLI - Scriptable Git interface

Usage: gitdesktop-cli <command> [options] [repo-path]

Commands:
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
  help                Show this help
```

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `dotnet: command not found` | Install .NET 10 SDK — see §A1 |
| `git: command not found` | Install git — see §A2 |
| Build error `net10.0` not recognized | Upgrade to .NET SDK 10.0+ |
| Docker: permission denied on `/repo` | Ensure the volume path is readable; on SELinux systems add `:z` to the volume flag: `-v "$(pwd)":/repo:z` |
| Docker: git credential prompts | The CLI sets `GIT_TERMINAL_PROMPT=0`; operations requiring credentials (push/pull to private remotes) may fail inside Docker.  Mount SSH keys or use HTTPS with a credential helper. |
| `Not a git repository` error | Pass the correct absolute path to the repository as the last argument |
