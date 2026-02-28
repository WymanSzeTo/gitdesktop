using GitDesktop.Core.Execution;
using GitDesktop.Core.Services;
using Xunit;

namespace GitDesktop.Core.Tests;

public class HistoryServiceTests
{
    [Fact]
    public async Task GetLogAsync_ReturnsCommits()
    {
        const string sep = "\x1f";
        const string rec = "\x1e";
        var mock = new MockGitExecutor();
        var line = string.Join(sep, new[]
        {
            "abc1234567890abc1234567890abc1234567890ab",
            "abc1234",
            "Fix bug",
            "",
            "Alice",
            "alice@example.com",
            "2024-01-15 10:00:00 +0000",
            "Alice",
            "alice@example.com",
            "2024-01-15 10:00:00 +0000",
            "",
        });
        mock.EnqueueSuccess(line + rec);

        var svc = new HistoryService(mock);
        var commits = await svc.GetLogAsync("/repo");

        Assert.Single(commits);
        Assert.Equal("abc1234", commits[0].ShortHash);
        Assert.Equal("Fix bug", commits[0].Subject);
        Assert.Equal("Alice", commits[0].AuthorName);
    }

    [Fact]
    public async Task GetLogAsync_WithPathSpec()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("");

        var svc = new HistoryService(mock);
        await svc.GetLogAsync("/repo", pathSpec: "src/file.cs");

        Assert.Contains("src/file.cs", mock.Calls[0].Arguments);
    }

    [Fact]
    public async Task GrepAsync_ParsesMatches()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("src/Foo.cs:10:public class Foo\nsrc/Bar.cs:5:// Foo reference\n");

        var svc = new HistoryService(mock);
        var result = await svc.GrepAsync("/repo", "Foo");

        Assert.Equal(2, result.Matches.Count);
        Assert.Equal("src/Foo.cs", result.Matches[0].FilePath);
        Assert.Equal(10, result.Matches[0].LineNumber);
    }

    [Fact]
    public async Task GetReflogAsync_ParsesEntries()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("abc1234|HEAD@{0}|commit: Initial|2024-01-15 10:00:00 +0000|Alice\ndef5678|HEAD@{1}|checkout: moving from main to dev|2024-01-14 09:00:00 +0000|Alice\n");

        var svc = new HistoryService(mock);
        var entries = await svc.GetReflogAsync("/repo");

        Assert.Equal(2, entries.Count);
        Assert.Equal("abc1234", entries[0].Hash);
        Assert.Equal("HEAD@{0}", entries[0].Ref);
    }

    [Fact]
    public async Task DiffAsync_CachedDiff()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("diff --cached output");

        var svc = new HistoryService(mock);
        await svc.DiffAsync("/repo", cached: true);

        Assert.Contains("--cached", mock.Calls[0].Arguments);
    }
}
