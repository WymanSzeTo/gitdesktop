using GitDesktop.Core.Execution;
using GitDesktop.Core.Services;
using Xunit;

namespace GitDesktop.Core.Tests;

public class MergeRebaseServiceTests
{
    [Fact]
    public async Task MergeAsync_FastForwardOnly()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new MergeRebaseService(mock);
        await svc.MergeAsync("/repo", "feature-branch", fastForwardOnly: true);

        Assert.Contains("--ff-only", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task RebaseAsync_Interactive()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new MergeRebaseService(mock);
        await svc.RebaseAsync("/repo", "main", interactive: true, autoSquash: true);

        Assert.Contains("-i", mock.Calls[0].Arguments);
        Assert.Contains("--autosquash", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task CherryPickAsync_MultipleCommits()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new MergeRebaseService(mock);
        await svc.CherryPickAsync("/repo", ["abc123", "def456"]);

        Assert.Contains("cherry-pick", mock.Calls[0].Arguments);
        Assert.Contains("abc123", mock.Calls[0].Arguments);
        Assert.Contains("def456", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task ResetAsync_HardReset()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new MergeRebaseService(mock);
        await svc.ResetAsync("/repo", "HEAD~3", MergeRebaseService.ResetMode.Hard);

        Assert.Contains("--hard", mock.Calls[0].Arguments);
        Assert.Contains("HEAD~3", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task RevertAsync_NoCommit()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new MergeRebaseService(mock);
        await svc.RevertAsync("/repo", "abc1234", noCommit: true);

        Assert.Contains("revert", mock.Calls[0].Arguments);
        Assert.Contains("-n", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task CheckoutConflictSideAsync_Ours()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new MergeRebaseService(mock);
        await svc.CheckoutConflictSideAsync("/repo", "file.txt", ours: true);

        Assert.Contains("--ours", mock.Calls[0].Arguments);
    }
}
