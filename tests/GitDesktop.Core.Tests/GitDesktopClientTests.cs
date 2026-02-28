using GitDesktop.Core;
using GitDesktop.Core.Execution;
using Xunit;

namespace GitDesktop.Core.Tests;

public class GitDesktopClientTests
{
    [Fact]
    public void Constructor_CreatesAllServices()
    {
        var mock = new MockGitExecutor();
        var client = new GitDesktopClient(mock);

        Assert.NotNull(client.Repository);
        Assert.NotNull(client.Commit);
        Assert.NotNull(client.Branch);
        Assert.NotNull(client.Remote);
        Assert.NotNull(client.History);
        Assert.NotNull(client.MergeRebase);
        Assert.NotNull(client.WorkTreeSubmodule);
        Assert.NotNull(client.Config);
        Assert.NotNull(client.Bisect);
        Assert.NotNull(client.Hooks);
        Assert.NotNull(client.Lfs);
        Assert.NotNull(client.Advanced);
    }

    [Fact]
    public void Constructor_DefaultExecutorIsProcessExecutor()
    {
        var client = new GitDesktopClient();
        Assert.IsType<GitProcessExecutor>(client.Executor);
    }

    [Fact]
    public void Constructor_UsesProvidedExecutor()
    {
        var mock = new MockGitExecutor();
        var client = new GitDesktopClient(mock);
        Assert.Same(mock, client.Executor);
    }
}
