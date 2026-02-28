using GitDesktop.Core.Execution;
using GitDesktop.Core.Services;
using Xunit;

namespace GitDesktop.Core.Tests;

public class RepositoryServiceTests
{
    [Fact]
    public async Task GetStatusAsync_ParsesBranchHead()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("# branch.head main\n# branch.upstream origin/main\n# branch.ab +2 -1\n");

        var svc = new RepositoryService(mock);
        var status = await svc.GetStatusAsync("/repo");

        Assert.Equal("main", status.CurrentBranch);
        Assert.Equal("origin/main", status.UpstreamBranch);
        Assert.Equal(2, status.AheadCount);
        Assert.Equal(1, status.BehindCount);
        Assert.False(status.IsDetachedHead);
    }

    [Fact]
    public async Task GetStatusAsync_DetectsDetachedHead()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("# branch.head (detached)\n");

        var svc = new RepositoryService(mock);
        var status = await svc.GetStatusAsync("/repo");

        Assert.True(status.IsDetachedHead);
    }

    [Fact]
    public async Task GetStatusAsync_ParsesUntrackedFile()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("# branch.head main\n? new_file.txt\n");

        var svc = new RepositoryService(mock);
        var status = await svc.GetStatusAsync("/repo");

        Assert.Single(status.Entries);
        Assert.Equal("new_file.txt", status.Entries[0].Path);
    }

    [Fact]
    public async Task GetStatusAsync_ParsesConflictedFile()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("# branch.head main\nu UU conflict.txt\n");

        var svc = new RepositoryService(mock);
        var status = await svc.GetStatusAsync("/repo");

        Assert.Single(status.Entries);
        Assert.True(status.Entries[0].IsConflicted);
    }

    [Fact]
    public async Task OpenAsync_ReturnsNullOnFailure()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueFailure("not a git repo", 128);

        var svc = new RepositoryService(mock);
        var repo = await svc.OpenAsync("/tmp/notarepo");

        Assert.Null(repo);
    }

    [Fact]
    public async Task OpenAsync_ParsesRepositoryInfo()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess(".git\nfalse\nmain");
        mock.EnqueueSuccess("git version 2.53.0");

        var svc = new RepositoryService(mock);
        var repo = await svc.OpenAsync("/repo");

        Assert.NotNull(repo);
        Assert.Equal("/repo", repo!.Path);
        Assert.False(repo.IsBare);
        Assert.Equal("main", repo.DefaultBranch);
    }

    [Fact]
    public async Task InitAsync_ExecutesCorrectCommand()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("Initialized empty Git repository");

        var svc = new RepositoryService(mock);
        await svc.InitAsync("/newrepo");

        Assert.Single(mock.Calls);
        Assert.Contains("init", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task CloneAsync_IncludesDepthWhenSpecified()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new RepositoryService(mock);
        await svc.CloneAsync("https://example.com/repo.git", "/dest", depth: 1);

        Assert.Contains("--depth 1", mock.Calls[0].Arguments);
    }
}
