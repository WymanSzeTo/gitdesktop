using GitDesktop.Core.Execution;
using GitDesktop.Core.Services;
using Xunit;

namespace GitDesktop.Core.Tests;

public class WorkTreeSubmoduleServiceTests
{
    [Fact]
    public async Task ListWorkTreesAsync_ParsesPortcelain()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("""
            worktree /home/user/project
            HEAD abc1234567890abc1234567890abc1234567890ab
            branch refs/heads/main

            worktree /home/user/project-feature
            HEAD def5678901234def5678901234def5678901234de
            branch refs/heads/feature/my-feature

            """);

        var svc = new WorkTreeSubmoduleService(mock);
        var worktrees = await svc.ListWorkTreesAsync("/repo");

        Assert.Equal(2, worktrees.Count);
        Assert.True(worktrees[0].IsMainWorktree);
        Assert.False(worktrees[1].IsMainWorktree);
    }

    [Fact]
    public async Task AddWorkTreeAsync_IncludesBranchFlag()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new WorkTreeSubmoduleService(mock);
        await svc.AddWorkTreeAsync("/repo", "/path/to/wt", branch: "feature");

        Assert.Contains("worktree add", mock.Calls[0].Arguments);
        Assert.Contains("-b", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task UpdateSubmodulesAsync_RecursiveInit()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new WorkTreeSubmoduleService(mock);
        await svc.UpdateSubmodulesAsync("/repo", recursive: true, init: true);

        Assert.Contains("submodule update", mock.Calls[0].Arguments);
        Assert.Contains("--init", mock.Calls[0].Arguments);
        Assert.Contains("--recursive", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task ListSubmodulesAsync_ParsesStatus()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess(" abc1234 lib/dep (v1.0)\n-def5678 tools/helper\n");

        var svc = new WorkTreeSubmoduleService(mock);
        var submodules = await svc.ListSubmodulesAsync("/repo");

        Assert.Equal(2, submodules.Count);
        Assert.Equal(Models.SubmoduleStatus.Uninitialized, submodules[1].Status);
    }
}
