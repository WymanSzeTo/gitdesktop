using GitDesktop.App.ViewModels;
using GitDesktop.Core;
using GitDesktop.Core.Execution;

namespace GitDesktop.App.Tests;

/// <summary>
/// Unit tests for <see cref="StashViewModel"/>.
/// </summary>
public class StashViewModelTests
{
    [Fact]
    public async Task RefreshAsync_PopulatesStashes()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("stash@{0}|WIP on main|abc1234|2024-01-01 12:00:00 +0000\nstash@{1}|Feature work|def5678|2024-01-02 12:00:00 +0000\n");

        var vm = new StashViewModel(new GitDesktopClient(mock), "/repo");
        await vm.RefreshAsync();

        Assert.Equal(2, vm.Stashes.Count);
        Assert.Equal("WIP on main", vm.Stashes[0].Message);
        Assert.Equal("Feature work", vm.Stashes[1].Message);
    }

    [Fact]
    public async Task RefreshAsync_NoStashes_EmptyCollection()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("");

        var vm = new StashViewModel(new GitDesktopClient(mock), "/repo");
        await vm.RefreshAsync();

        Assert.Empty(vm.Stashes);
    }

    [Fact]
    public void SelectedStash_Set_RaisesPropertyChanged()
    {
        var mock = new MockGitExecutor();
        // Enqueue response for StashShowAsync triggered when SelectedStash is set
        mock.EnqueueSuccess("diff output");
        var vm = new StashViewModel(new GitDesktopClient(mock), "/repo");

        var changedProperties = new List<string?>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        var stash = new GitDesktop.Core.Models.Stash { Index = 0, Ref = "stash@{0}", Message = "WIP" };
        vm.SelectedStash = stash;

        Assert.Contains(nameof(StashViewModel.SelectedStash), changedProperties);
    }

    [Fact]
    public void StashMessage_Set_RaisesPropertyChanged()
    {
        var mock = new MockGitExecutor();
        var vm = new StashViewModel(new GitDesktopClient(mock), "/repo");

        string? changedProperty = null;
        vm.PropertyChanged += (_, e) => changedProperty = e.PropertyName;

        vm.StashMessage = "My stash message";

        Assert.Equal(nameof(StashViewModel.StashMessage), changedProperty);
    }
}
