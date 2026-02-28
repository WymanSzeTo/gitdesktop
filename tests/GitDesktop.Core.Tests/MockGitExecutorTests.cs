using GitDesktop.Core.Execution;
using Xunit;

namespace GitDesktop.Core.Tests;

public class MockGitExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsEnqueuedResult()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("branch output");

        var result = await mock.ExecuteAsync("/repo", "branch");

        Assert.True(result.Success);
        Assert.Equal("branch output", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_RecordsCalls()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        await mock.ExecuteAsync("/repo", "status");

        Assert.Single(mock.Calls);
        Assert.Equal("/repo", mock.Calls[0].WorkingDirectory);
        Assert.Equal("status", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsDefaultOkWhenQueueEmpty()
    {
        var mock = new MockGitExecutor();

        var result = await mock.ExecuteAsync("/repo", "version");

        Assert.True(result.Success);
    }

    [Fact]
    public async Task EnqueueFailure_ReturnsFailureResult()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueFailure("fatal: not a git repository", 128);

        var result = await mock.ExecuteAsync("/tmp", "status");

        Assert.False(result.Success);
        Assert.Equal(128, result.ExitCode);
        Assert.Contains("not a git repository", result.Error);
    }

    [Fact]
    public async Task ExecuteWithInputAsync_RecordsCalls()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        await mock.ExecuteWithInputAsync("/repo", "commit --file=-", "commit message");

        Assert.Single(mock.Calls);
        Assert.Equal("commit --file=-", mock.Calls[0].Arguments);
    }
}
