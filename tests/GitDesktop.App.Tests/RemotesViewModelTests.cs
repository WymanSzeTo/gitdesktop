using GitDesktop.App.ViewModels;
using GitDesktop.Core;
using GitDesktop.Core.Execution;

namespace GitDesktop.App.Tests;

/// <summary>
/// Unit tests for <see cref="RemotesViewModel"/>.
/// </summary>
public class RemotesViewModelTests
{
    [Fact]
    public async Task RefreshAsync_PopulatesRemotes()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("origin\thttps://github.com/user/repo (fetch)\norigin\thttps://github.com/user/repo (push)\n");

        var vm = new RemotesViewModel(new GitDesktopClient(mock), "/repo");
        await vm.RefreshAsync();

        Assert.Single(vm.Remotes);
        Assert.Equal("origin", vm.Remotes[0].Name);
        Assert.Equal("https://github.com/user/repo", vm.Remotes[0].FetchUrl);
    }

    [Fact]
    public async Task RefreshAsync_NoRemotes_EmptyCollection()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess("");

        var vm = new RemotesViewModel(new GitDesktopClient(mock), "/repo");
        await vm.RefreshAsync();

        Assert.Empty(vm.Remotes);
    }

    [Fact]
    public void AddRemoteCommand_CannotExecute_WhenNameEmpty()
    {
        var mock = new MockGitExecutor();
        var vm = new RemotesViewModel(new GitDesktopClient(mock), "/repo");

        vm.NewRemoteName = string.Empty;
        vm.NewRemoteUrl = "https://example.com/repo";

        Assert.False(vm.AddRemoteCommand.CanExecute(null));
    }

    [Fact]
    public void AddRemoteCommand_CannotExecute_WhenUrlEmpty()
    {
        var mock = new MockGitExecutor();
        var vm = new RemotesViewModel(new GitDesktopClient(mock), "/repo");

        vm.NewRemoteName = "upstream";
        vm.NewRemoteUrl = string.Empty;

        Assert.False(vm.AddRemoteCommand.CanExecute(null));
    }

    [Fact]
    public void AddRemoteCommand_CanExecute_WhenNameAndUrlProvided()
    {
        var mock = new MockGitExecutor();
        var vm = new RemotesViewModel(new GitDesktopClient(mock), "/repo");

        vm.NewRemoteName = "upstream";
        vm.NewRemoteUrl = "https://example.com/repo";

        Assert.True(vm.AddRemoteCommand.CanExecute(null));
    }

    [Fact]
    public void NewRemoteName_Set_RaisesPropertyChanged()
    {
        var mock = new MockGitExecutor();
        var vm = new RemotesViewModel(new GitDesktopClient(mock), "/repo");

        string? changedProperty = null;
        vm.PropertyChanged += (_, e) => changedProperty = e.PropertyName;

        vm.NewRemoteName = "upstream";

        Assert.Equal(nameof(RemotesViewModel.NewRemoteName), changedProperty);
    }
}
