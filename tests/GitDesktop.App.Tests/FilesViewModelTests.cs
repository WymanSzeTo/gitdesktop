using GitDesktop.App.ViewModels;
using GitDesktop.Core;
using GitDesktop.Core.Execution;

namespace GitDesktop.App.Tests;

/// <summary>
/// Unit tests for <see cref="FilesViewModel"/>.
/// </summary>
public class FilesViewModelTests
{
    [Fact]
    public async Task RefreshAsync_PopulatesAllFiles()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("README.md\nsrc/Program.cs\nsrc/Utils.cs\n");

        var vm = new FilesViewModel(new GitDesktopClient(mock), "/repo");
        await vm.RefreshAsync();

        Assert.Equal(3, vm.AllFiles.Count);
        Assert.Contains("README.md", vm.AllFiles);
        Assert.Contains("src/Program.cs", vm.AllFiles);
    }

    [Fact]
    public async Task FilterText_Set_FiltersFileList()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("README.md\nsrc/Program.cs\nsrc/Utils.cs\n");

        var vm = new FilesViewModel(new GitDesktopClient(mock), "/repo");
        await vm.RefreshAsync();

        vm.FilterText = "src";

        Assert.Equal(2, vm.FilteredFiles.Count);
        Assert.DoesNotContain("README.md", vm.FilteredFiles);
    }

    [Fact]
    public async Task FilterText_Empty_ShowsAllFiles()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("README.md\nsrc/Program.cs\n");

        var vm = new FilesViewModel(new GitDesktopClient(mock), "/repo");
        await vm.RefreshAsync();

        vm.FilterText = "src";
        vm.FilterText = string.Empty;

        Assert.Equal(2, vm.FilteredFiles.Count);
    }

    [Fact]
    public async Task RefreshAsync_GitFailure_LeavesListEmpty()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueFailure("fatal: not a git repo", 128);

        var vm = new FilesViewModel(new GitDesktopClient(mock), "/bad");
        await vm.RefreshAsync();

        Assert.Empty(vm.AllFiles);
        Assert.Empty(vm.FilteredFiles);
    }

    [Fact]
    public void FilterText_Set_RaisesPropertyChanged()
    {
        var mock = new MockGitExecutor();
        var vm   = new FilesViewModel(new GitDesktopClient(mock), "/repo");

        string? prop = null;
        vm.PropertyChanged += (_, e) => prop = e.PropertyName;

        vm.FilterText = "hello";

        Assert.Equal(nameof(FilesViewModel.FilterText), prop);
    }
}
