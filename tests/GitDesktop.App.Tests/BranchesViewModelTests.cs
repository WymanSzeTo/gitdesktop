using GitDesktop.App.ViewModels;
using GitDesktop.Core;
using GitDesktop.Core.Execution;

namespace GitDesktop.App.Tests;

/// <summary>
/// Unit tests for <see cref="BranchesViewModel"/>.
/// </summary>
public class BranchesViewModelTests
{
    [Fact]
    public async Task RefreshAsync_PopulatesBranches()
    {
        var mock = new MockGitExecutor();
        // Format: %(refname:short)|%(objectname:short)|%(HEAD)|%(upstream:short)|%(upstream:track)
        mock.EnqueueSuccess("main|abc1234|*||\nfeature/foo|def5678| ||\n");

        var vm = new BranchesViewModel(new GitDesktopClient(mock), "/repo");
        await vm.RefreshAsync();

        Assert.Equal(2, vm.Branches.Count);
        Assert.Equal("main", vm.Branches[0].Name);
        Assert.True(vm.Branches[0].IsCurrentBranch);
        Assert.Equal("feature/foo", vm.Branches[1].Name);
    }

    [Fact]
    public async Task RefreshAsync_NoBranches_EmptyCollection()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("");

        var vm = new BranchesViewModel(new GitDesktopClient(mock), "/repo");
        await vm.RefreshAsync();

        Assert.Empty(vm.Branches);
    }

    [Fact]
    public void CreateBranchCommand_CannotExecute_WhenNameEmpty()
    {
        var mock = new MockGitExecutor();
        var vm = new BranchesViewModel(new GitDesktopClient(mock), "/repo");

        vm.NewBranchName = string.Empty;

        Assert.False(vm.CreateBranchCommand.CanExecute(null));
    }

    [Fact]
    public void CreateBranchCommand_CanExecute_WhenNameProvided()
    {
        var mock = new MockGitExecutor();
        var vm = new BranchesViewModel(new GitDesktopClient(mock), "/repo");

        vm.NewBranchName = "feature/new";

        Assert.True(vm.CreateBranchCommand.CanExecute(null));
    }

    [Fact]
    public void NewBranchName_Set_RaisesPropertyChanged()
    {
        var mock = new MockGitExecutor();
        var vm = new BranchesViewModel(new GitDesktopClient(mock), "/repo");

        string? changedProperty = null;
        vm.PropertyChanged += (_, e) => changedProperty = e.PropertyName;

        vm.NewBranchName = "my-branch";

        Assert.Equal(nameof(BranchesViewModel.NewBranchName), changedProperty);
    }
}
