using GitDesktop.Core.Execution;
using GitDesktop.Core.Services;
using Xunit;

namespace GitDesktop.Core.Tests;

public class BranchServiceTests
{
    [Fact]
    public async Task CreateAsync_UsesCorrectArguments()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new BranchService(mock);
        await svc.CreateAsync("/repo", "feature/my-feature", "main");

        Assert.Contains("branch", mock.Calls[0].Arguments);
        Assert.Contains("feature/my-feature", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task DeleteAsync_SafeDelete()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new BranchService(mock);
        await svc.DeleteAsync("/repo", "old-branch", force: false);

        Assert.Contains("-d", mock.Calls[0].Arguments);
        Assert.DoesNotContain("-D", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task DeleteAsync_ForceDelete()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new BranchService(mock);
        await svc.DeleteAsync("/repo", "old-branch", force: true);

        Assert.Contains("-D", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task RenameAsync_UsesCorrectArguments()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new BranchService(mock);
        await svc.RenameAsync("/repo", "old-name", "new-name");

        Assert.Contains("-m", mock.Calls[0].Arguments);
        Assert.Contains("old-name", mock.Calls[0].Arguments);
        Assert.Contains("new-name", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task CreateAnnotatedTagAsync_IncludesAnnotationFlag()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new BranchService(mock);
        await svc.CreateAnnotatedTagAsync("/repo", "v1.0.0", "Release 1.0.0");

        Assert.Contains("tag -a", mock.Calls[0].Arguments);
        Assert.Contains("-m", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task CreateAnnotatedTagAsync_IncludesSignFlag()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new BranchService(mock);
        await svc.CreateAnnotatedTagAsync("/repo", "v1.0.0", "Release", sign: true);

        Assert.Contains("-s", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task SwitchAsync_WithCreate()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess();

        var svc = new BranchService(mock);
        await svc.SwitchAsync("/repo", "new-branch", createIfNotExists: true);

        Assert.Contains("-C", mock.Calls[0].Arguments);
    }
}
