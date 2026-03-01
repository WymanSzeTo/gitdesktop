using GitDesktop.App.ViewModels;
using GitDesktop.Core;
using GitDesktop.Core.Execution;
using GitDesktop.Core.Models;

namespace GitDesktop.App.Tests;

/// <summary>
/// Unit tests for the diff-highlighting additions to <see cref="StatusViewModel"/>.
/// </summary>
public class DiffHighlightingTests
{
    [Fact]
    public void ParseDiffLines_AddedLine_ReturnsAddedType()
    {
        var lines = StatusViewModel.ParseDiffLines("+new line").ToList();

        Assert.Single(lines);
        Assert.Equal(DiffLineType.Added, lines[0].Type);
    }

    [Fact]
    public void ParseDiffLines_RemovedLine_ReturnsRemovedType()
    {
        var lines = StatusViewModel.ParseDiffLines("-old line").ToList();

        Assert.Single(lines);
        Assert.Equal(DiffLineType.Removed, lines[0].Type);
    }

    [Fact]
    public void ParseDiffLines_HunkHeader_ReturnsHunkType()
    {
        var lines = StatusViewModel.ParseDiffLines("@@ -1,4 +1,5 @@").ToList();

        Assert.Single(lines);
        Assert.Equal(DiffLineType.Hunk, lines[0].Type);
    }

    [Fact]
    public void ParseDiffLines_DiffHeader_ReturnsHeaderType()
    {
        var input = "diff --git a/file.txt b/file.txt";
        var lines = StatusViewModel.ParseDiffLines(input).ToList();

        Assert.Single(lines);
        Assert.Equal(DiffLineType.Header, lines[0].Type);
    }

    [Fact]
    public void ParseDiffLines_PlusPlusPlus_ReturnsHeaderType()
    {
        var lines = StatusViewModel.ParseDiffLines("+++ b/file.txt").ToList();

        Assert.Single(lines);
        Assert.Equal(DiffLineType.Header, lines[0].Type);
    }

    [Fact]
    public void ParseDiffLines_ContextLine_ReturnsContextType()
    {
        var lines = StatusViewModel.ParseDiffLines(" unchanged line").ToList();

        Assert.Single(lines);
        Assert.Equal(DiffLineType.Context, lines[0].Type);
    }

    [Fact]
    public void ParseDiffLines_MultipleLines_CorrectTypes()
    {
        var diff = "diff --git a/f b/f\n--- a/f\n+++ b/f\n@@ -1 +1 @@\n-old\n+new\n ctx";
        var lines = StatusViewModel.ParseDiffLines(diff).ToList();

        Assert.Equal(DiffLineType.Header,  lines[0].Type);
        Assert.Equal(DiffLineType.Header,  lines[1].Type);
        Assert.Equal(DiffLineType.Header,  lines[2].Type);
        Assert.Equal(DiffLineType.Hunk,    lines[3].Type);
        Assert.Equal(DiffLineType.Removed, lines[4].Type);
        Assert.Equal(DiffLineType.Added,   lines[5].Type);
    }

    [Fact]
    public void DiffLineViewModel_AddedLine_HasCorrectResourceKeys()
    {
        var vm = new DiffLineViewModel(new DiffLine { Content = "+new", Type = DiffLineType.Added });

        Assert.Equal("ThemeAddedBackground", vm.BackgroundKey);
        Assert.Equal("ThemeAddedForeground", vm.ForegroundKey);
    }

    [Fact]
    public void DiffLineViewModel_RemovedLine_HasCorrectResourceKeys()
    {
        var vm = new DiffLineViewModel(new DiffLine { Content = "-old", Type = DiffLineType.Removed });

        Assert.Equal("ThemeRemovedBackground", vm.BackgroundKey);
        Assert.Equal("ThemeRemovedForeground", vm.ForegroundKey);
    }

    [Fact]
    public void DiffLineViewModel_HunkLine_HasCorrectResourceKeys()
    {
        var vm = new DiffLineViewModel(new DiffLine { Content = "@@ -1 +1 @@", Type = DiffLineType.Hunk });

        Assert.Equal("ThemeHunkBackground", vm.BackgroundKey);
        Assert.Equal("ThemeHunkForeground", vm.ForegroundKey);
    }

    [Fact]
    public async Task SelectedFile_Set_LoadsDiffLines()
    {
        var mock = new MockGitExecutor();
        // DiffAsync response (cached=false because AddedForeground is still "Added" from IndexStatus)
        mock.EnqueueSuccess("+added line\n-removed line\n context line\n");

        var vm    = new StatusViewModel(new GitDesktopClient(mock), "/repo");
        var entry = new StatusEntry
        {
            Path           = "file.txt",
            IndexStatus    = FileStatusKind.Added,
            WorkTreeStatus = FileStatusKind.Modified,
        };

        // Call LoadDiffAsync directly so the test can await completion reliably.
        await vm.LoadDiffAsync(entry);

        Assert.NotEmpty(vm.DiffLines);
        Assert.Equal(DiffLineType.Added,   vm.DiffLines[0].Type);
        Assert.Equal(DiffLineType.Removed, vm.DiffLines[1].Type);
        Assert.Equal(DiffLineType.Context, vm.DiffLines[2].Type);
    }
}
