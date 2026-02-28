using GitDesktop.Core.Execution;
using GitDesktop.Core.Services;
using Xunit;

namespace GitDesktop.Core.Tests;

public class RemoteServiceTests
{
    [Fact]
    public async Task ListRemotesAsync_ParsesFetchAndPush()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("origin\thttps://github.com/user/repo.git (fetch)\norigin\thttps://github.com/user/repo.git (push)\n");

        var svc = new RemoteService(mock);
        var remotes = await svc.ListRemotesAsync("/repo");

        Assert.Single(remotes);
        Assert.Equal("origin", remotes[0].Name);
        Assert.Equal("https://github.com/user/repo.git", remotes[0].FetchUrl);
    }

    [Fact]
    public async Task AddAsync_IncludesNameAndUrl()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new RemoteService(mock);
        await svc.AddAsync("/repo", "upstream", "https://github.com/org/repo.git");

        Assert.Contains("remote add", mock.Calls[0].Arguments);
        Assert.Contains("upstream", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task PushAsync_ForceWithLease()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new RemoteService(mock);
        await svc.PushAsync("/repo", forceWithLease: true);

        Assert.Contains("--force-with-lease", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task FetchAsync_AllRemotes()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new RemoteService(mock);
        await svc.FetchAsync("/repo", all: true, prune: true);

        Assert.Contains("--all", mock.Calls[0].Arguments);
        Assert.Contains("--prune", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task PullAsync_RebaseStrategy()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new RemoteService(mock);
        await svc.PullAsync("/repo", strategy: RemoteService.PullStrategy.Rebase);

        Assert.Contains("--rebase", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task PushAsync_SetUpstream()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new RemoteService(mock);
        await svc.PushAsync("/repo", setUpstream: true);

        Assert.Contains("-u", mock.Calls[0].Arguments);
    }
}
