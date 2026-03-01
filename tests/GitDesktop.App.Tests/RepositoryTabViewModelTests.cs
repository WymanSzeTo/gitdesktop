using GitDesktop.App.ViewModels;
using GitDesktop.Core;
using GitDesktop.Core.Execution;

namespace GitDesktop.App.Tests;

/// <summary>
/// Unit tests for <see cref="RepositoryTabViewModel"/>.
/// </summary>
public class RepositoryTabViewModelTests
{
    [Fact]
    public void Constructor_InitialisesChildViewModels()
    {
        var mock = new MockGitExecutor();
        var tab  = new RepositoryTabViewModel(new GitDesktopClient(mock), "/repo", "repo");

        Assert.NotNull(tab.StatusVM);
        Assert.NotNull(tab.BranchesVM);
        Assert.NotNull(tab.HistoryVM);
        Assert.NotNull(tab.TagsVM);
        Assert.NotNull(tab.RemotesVM);
        Assert.NotNull(tab.StashVM);
        Assert.NotNull(tab.FilesVM);
    }

    [Fact]
    public void Constructor_DefaultsToStatusView()
    {
        var mock = new MockGitExecutor();
        var tab  = new RepositoryTabViewModel(new GitDesktopClient(mock), "/repo", "repo");

        Assert.IsType<StatusViewModel>(tab.CurrentView);
    }

    [Fact]
    public void ShowFilesCommand_SwitchesToFilesView()
    {
        var mock = new MockGitExecutor();
        var tab  = new RepositoryTabViewModel(new GitDesktopClient(mock), "/repo", "repo");

        tab.ShowFilesCommand.Execute(null);

        Assert.IsType<FilesViewModel>(tab.CurrentView);
    }

    [Fact]
    public void ShowStatusCommand_SwitchesToStatusView()
    {
        var mock = new MockGitExecutor();
        var tab  = new RepositoryTabViewModel(new GitDesktopClient(mock), "/repo", "repo");

        tab.ShowFilesCommand.Execute(null);   // navigate away
        tab.ShowStatusCommand.Execute(null);

        Assert.IsType<StatusViewModel>(tab.CurrentView);
    }

    [Fact]
    public void Name_Set_RaisesPropertyChanged()
    {
        var mock    = new MockGitExecutor();
        var tab     = new RepositoryTabViewModel(new GitDesktopClient(mock), "/repo", "repo");
        string? prop = null;
        tab.PropertyChanged += (_, e) => prop = e.PropertyName;

        tab.Name = "new-name";

        Assert.Equal(nameof(RepositoryTabViewModel.Name), prop);
    }

    [Fact]
    public async Task LoadAsync_CallsRefreshOnChildViewModels()
    {
        var mock = new MockGitExecutor();
        // Responses: GetStatus, ListBranches, GetLog
        mock.EnqueueSuccess("# branch.head main\n");
        mock.EnqueueSuccess("main|abc1234|true||0|0\n");
        mock.EnqueueSuccess("");

        var tab = new RepositoryTabViewModel(new GitDesktopClient(mock), "/repo", "repo");
        await tab.LoadAsync();

        Assert.Equal("main", tab.StatusVM.CurrentBranch);
    }
}
