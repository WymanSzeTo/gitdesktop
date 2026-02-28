using GitDesktop.App.ViewModels;
using GitDesktop.Core;
using GitDesktop.Core.Execution;

namespace GitDesktop.App.Tests;

/// <summary>
/// Unit tests for <see cref="StatusViewModel"/>.
/// </summary>
public class StatusViewModelTests
{
    [Fact]
    public async Task RefreshAsync_PopulatesBranchAndFiles()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess(
            "# branch.head main\n" +
            "# branch.upstream origin/main\n" +
            "# branch.ab +1 -2\n" +
            "1 M. N... 100644 100644 100644 aaa bbb modified.txt\n" +
            "? untracked.txt\n");

        var vm = new StatusViewModel(new GitDesktopClient(mock), "/repo");
        await vm.RefreshAsync();

        Assert.Equal("main", vm.CurrentBranch);
        Assert.Equal("origin/main", vm.UpstreamBranch);
        Assert.Equal(1, vm.AheadCount);
        Assert.Equal(2, vm.BehindCount);
        Assert.Single(vm.UntrackedFiles);
        Assert.Equal("untracked.txt", vm.UntrackedFiles[0].Path);
    }

    [Fact]
    public async Task RefreshAsync_EmptyRepo_AllCollectionsEmpty()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("# branch.head main\n");

        var vm = new StatusViewModel(new GitDesktopClient(mock), "/repo");
        await vm.RefreshAsync();

        Assert.Equal("main", vm.CurrentBranch);
        Assert.Empty(vm.StagedFiles);
        Assert.Empty(vm.UnstagedFiles);
        Assert.Empty(vm.UntrackedFiles);
    }

    [Fact]
    public void CommitCommand_CannotExecute_WhenMessageEmpty()
    {
        var mock = new MockGitExecutor();
        var vm = new StatusViewModel(new GitDesktopClient(mock), "/repo");

        vm.CommitMessage = string.Empty;

        Assert.False(vm.CommitCommand.CanExecute(null));
    }

    [Fact]
    public void CommitMessage_Set_RaisesPropertyChanged()
    {
        var mock = new MockGitExecutor();
        var vm = new StatusViewModel(new GitDesktopClient(mock), "/repo");

        string? changedProperty = null;
        vm.PropertyChanged += (_, e) => changedProperty = e.PropertyName;

        vm.CommitMessage = "test message";

        Assert.Equal(nameof(StatusViewModel.CommitMessage), changedProperty);
    }
}
