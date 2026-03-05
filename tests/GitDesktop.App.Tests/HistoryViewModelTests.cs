using GitDesktop.App.ViewModels;
using GitDesktop.Core;
using GitDesktop.Core.Execution;
using GitDesktop.Core.Models;

namespace GitDesktop.App.Tests;

/// <summary>
/// Unit tests for <see cref="HistoryViewModel"/>.
/// </summary>
public class HistoryViewModelTests
{
    private static string BuildLogOutput(int count = 2)
    {
        var sep = '\x1e';
        var us = '\x1f';
        var entries = Enumerable.Range(1, count).Select(i =>
            $"abc{i:D4}{us}abc{i}{us}Subject {i}{us}{us}Author{i}{us}author{i}@example.com{us}" +
            $"2024-01-0{i} 12:00:00 +0000{us}Committer{i}{us}committer{i}@example.com{us}" +
            $"2024-01-0{i} 12:00:00 +0000{us}parent{i}{us}N{us}");
        return string.Join(sep.ToString(), entries);
    }

    [Fact]
    public async Task RefreshAsync_PopulatesCommits()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess(BuildLogOutput(3));

        var vm = new HistoryViewModel(new GitDesktopClient(mock), "/repo");
        await vm.RefreshAsync();

        Assert.Equal(3, vm.Commits.Count);
        Assert.Equal("Subject 1", vm.Commits[0].Subject);
    }

    [Fact]
    public async Task RefreshAsync_NoCommits_EmptyCollection()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("");

        var vm = new HistoryViewModel(new GitDesktopClient(mock), "/repo");
        await vm.RefreshAsync();

        Assert.Empty(vm.Commits);
    }

    [Fact]
    public void SelectedCommit_Set_RaisesPropertyChanged()
    {
        var mock = new MockGitExecutor();
        // Enqueue a response for the ShowAsync call triggered when SelectedCommit is set
        mock.EnqueueSuccess("diff output");
        var vm = new HistoryViewModel(new GitDesktopClient(mock), "/repo");

        var changedProperties = new List<string?>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        var commit = new GitDesktop.Core.Models.Commit { Hash = "abc1234", Subject = "Test" };
        vm.SelectedCommit = commit;

        Assert.Contains(nameof(HistoryViewModel.SelectedCommit), changedProperties);
    }

    [Fact]
    public void StatusMessage_InitialValue_IsNull()
    {
        var mock = new MockGitExecutor();
        var vm = new HistoryViewModel(new GitDesktopClient(mock), "/repo");

        Assert.Null(vm.StatusMessage);
    }

    [Fact]
    public async Task SelectedCommit_Set_BuildsDiffLines()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("diff --git a/a.cs b/a.cs\n+++ b/a.cs\n@@ -1 +1 @@\n+public class Foo {}\n");
        var vm = new HistoryViewModel(new GitDesktopClient(mock), "/repo");

        vm.SelectedCommit = new GitDesktop.Core.Models.Commit { Hash = "abc1234", Subject = "Test" };
        await Task.Delay(25);

        Assert.NotEmpty(vm.SelectedCommitDiffLines);
        Assert.Equal("CSharp", vm.DetectedDiffLanguage);
    }

    [Fact]
    public void HistoryDiffLineViewModel_ContextKeyword_UsesSyntaxColor()
    {
        var vm = new HistoryDiffLineViewModel("public class Foo", DiffLineType.Context, FileLineKind.Keyword);
        Assert.Equal("ThemeSyntaxKeyword", vm.ForegroundKey);
    }

    [Fact]
    public async Task SelectedCommit_WithMultipleFileLanguages_SetsMixedLanguage()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("diff --git a/a.cs b/a.cs\n+++ b/a.cs\n+public class Foo {}\ndiff --git a/b.py b/b.py\n+++ b/b.py\n+def bar():\n");
        var vm = new HistoryViewModel(new GitDesktopClient(mock), "/repo");

        vm.SelectedCommit = new Commit { Hash = "abc9999", Subject = "Test" };
        await Task.Delay(25);

        Assert.Equal("Mixed", vm.DetectedDiffLanguage);
    }
}
