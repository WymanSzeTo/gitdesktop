using GitDesktop.Core.Execution;
using GitDesktop.Core.Services;
using Xunit;

namespace GitDesktop.Core.Tests;

public class CommitServiceTests
{
    [Fact]
    public async Task CommitAsync_UsesMessageFlag()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("[main abc1234] My commit");

        var svc = new CommitService(mock);
        var result = await svc.CommitAsync("/repo", "My commit");

        Assert.True(result.Success);
        Assert.Contains("-m", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task CommitAsync_IncludesSignFlag()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new CommitService(mock);
        await svc.CommitAsync("/repo", "signed commit", sign: true);

        Assert.Contains(" -S", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task AmendAsync_NoEdit()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new CommitService(mock);
        await svc.AmendAsync("/repo", noEdit: true);

        Assert.Contains("--amend", mock.Calls[0].Arguments);
        Assert.Contains("--no-edit", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task StashPushAsync_IncludesUntrackedFlag()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new CommitService(mock);
        await svc.StashPushAsync("/repo", includeUntracked: true);

        Assert.Contains("-u", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task StashListAsync_ParsesOutput()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("stash@{0}|WIP on main: fix|abc123|2024-01-15 10:00:00 +0000\nstash@{1}|WIP on dev: wip|def456|2024-01-14 09:00:00 +0000\n");

        var svc = new CommitService(mock);
        var stashes = await svc.StashListAsync("/repo");

        Assert.Equal(2, stashes.Count);
        Assert.Equal("stash@{0}", stashes[0].Ref);
        Assert.Equal("WIP on main: fix", stashes[0].Message);
    }

    [Fact]
    public async Task CleanAsync_IncludesForceFlag()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new CommitService(mock);
        await svc.CleanAsync("/repo", force: true);

        Assert.Contains("-f", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task RestoreAsync_StagedFlag()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new CommitService(mock);
        await svc.RestoreAsync("/repo", "file.txt", staged: true);

        Assert.Contains("--staged", mock.Calls[0].Arguments);
    }
}
