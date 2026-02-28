using GitDesktop.Core.Execution;
using Xunit;

namespace GitDesktop.Core.Tests;

public class GitResultTests
{
    [Fact]
    public void Ok_ReturnsSuccessResult()
    {
        var result = GitResult.Ok("output");
        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("output", result.Output);
        Assert.Empty(result.Error);
    }

    [Fact]
    public void Fail_ReturnsFailureResult()
    {
        var result = GitResult.Fail("some error", 128);
        Assert.False(result.Success);
        Assert.Equal(128, result.ExitCode);
        Assert.Equal("some error", result.Error);
    }

    [Fact]
    public void DefaultResult_IsNotSuccess()
    {
        var result = new GitResult { ExitCode = 1 };
        Assert.False(result.Success);
    }
}
