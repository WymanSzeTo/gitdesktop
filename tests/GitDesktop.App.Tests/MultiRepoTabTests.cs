using GitDesktop.App.Services;
using GitDesktop.App.ViewModels;
using GitDesktop.Core;
using GitDesktop.Core.Execution;

namespace GitDesktop.App.Tests;

/// <summary>
/// Unit tests for multi-repository tab management in <see cref="MainWindowViewModel"/>.
/// </summary>
public class MultiRepoTabTests : IDisposable
{
    private readonly string _tempDir;

    public MultiRepoTabTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"GitDesktop_MultiRepoTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private AppConfigService IsolatedConfig() =>
        new(Path.Combine(_tempDir, $"cfg_{Guid.NewGuid():N}.json"));

    private static MockGitExecutor ValidRepoMock()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess(".git\nfalse\nmain");     // OpenAsync rev-parse
        mock.EnqueueSuccess("git version 2.40.0");   // OpenAsync git version
        mock.EnqueueSuccess("# branch.head main\n"); // StatusVM refresh
        mock.EnqueueSuccess("main|abc1234|true||0|0\n"); // BranchesVM refresh
        mock.EnqueueSuccess("");                      // HistoryVM refresh
        return mock;
    }

    [Fact]
    public async Task OpenRepositoryAsync_AddsNewTab()
    {
        var mock = ValidRepoMock();
        var vm   = new MainWindowViewModel(new GitDesktopClient(mock), IsolatedConfig());
        vm.RepoPath = "/repo-a";

        await vm.OpenRepositoryAsync();

        Assert.Single(vm.Tabs);
        Assert.NotNull(vm.SelectedTab);
        Assert.Equal("/repo-a", vm.SelectedTab!.RepoPath);
    }

    [Fact]
    public async Task OpenRepositoryAsync_TwiceDifferentPaths_CreatesTwoTabs()
    {
        var mock = new MockGitExecutor();
        // First repo
        mock.EnqueueSuccess(".git\nfalse\nmain"); mock.EnqueueSuccess("git version 2.40.0");
        mock.EnqueueSuccess("# branch.head main\n");
        mock.EnqueueSuccess("main|abc1234|true||0|0\n");
        mock.EnqueueSuccess("");
        // Second repo
        mock.EnqueueSuccess(".git\nfalse\ndev"); mock.EnqueueSuccess("git version 2.40.0");
        mock.EnqueueSuccess("# branch.head dev\n");
        mock.EnqueueSuccess("dev|def5678|true||0|0\n");
        mock.EnqueueSuccess("");

        var vm = new MainWindowViewModel(new GitDesktopClient(mock), IsolatedConfig());

        vm.RepoPath = "/repo-a";
        await vm.OpenRepositoryAsync();

        vm.RepoPath = "/repo-b";
        await vm.OpenRepositoryAsync();

        Assert.Equal(2, vm.Tabs.Count);
    }

    [Fact]
    public async Task CloseTabCommand_RemovesTab()
    {
        var mock = ValidRepoMock();
        var vm   = new MainWindowViewModel(new GitDesktopClient(mock), IsolatedConfig());
        vm.RepoPath = "/repo-a";
        await vm.OpenRepositoryAsync();

        var tab = vm.SelectedTab!;
        vm.CloseTabCommand.Execute(tab);

        Assert.Empty(vm.Tabs);
        Assert.Null(vm.SelectedTab);
    }

    [Fact]
    public async Task SelectedTab_Set_UpdatesTitle()
    {
        var mock = new MockGitExecutor();
        mock.EnqueueSuccess(".git\nfalse\nmain"); mock.EnqueueSuccess("git version 2.40.0");
        mock.EnqueueSuccess("# branch.head main\n");
        mock.EnqueueSuccess("main|abc1234|true||0|0\n"); mock.EnqueueSuccess("");
        mock.EnqueueSuccess(".git\nfalse\nmain"); mock.EnqueueSuccess("git version 2.40.0");
        mock.EnqueueSuccess("# branch.head main\n");
        mock.EnqueueSuccess("main|abc1234|true||0|0\n"); mock.EnqueueSuccess("");

        var vm = new MainWindowViewModel(new GitDesktopClient(mock), IsolatedConfig());

        vm.RepoPath = "/repo-a"; await vm.OpenRepositoryAsync();
        vm.RepoPath = "/repo-b"; await vm.OpenRepositoryAsync();

        vm.SelectedTab = vm.Tabs[0];

        Assert.Contains("/repo-a", vm.Title);
    }

    [Fact]
    public async Task SelectedTab_PassesThroughStatusVM()
    {
        var mock = ValidRepoMock();
        var vm   = new MainWindowViewModel(new GitDesktopClient(mock), IsolatedConfig());
        vm.RepoPath = "/repo-a";
        await vm.OpenRepositoryAsync();

        Assert.Same(vm.SelectedTab!.StatusVM, vm.StatusVM);
    }

    [Fact]
    public void SelectedTheme_DefaultIsDark()
    {
        var vm = new MainWindowViewModel(new GitDesktopClient(new MockGitExecutor()), IsolatedConfig());

        Assert.Equal("Dark", vm.SelectedTheme);
    }

    [Fact]
    public void FontSize_DefaultIs13()
    {
        var vm = new MainWindowViewModel(new GitDesktopClient(new MockGitExecutor()), IsolatedConfig());

        Assert.Equal(13.0, vm.FontSize);
    }

    [Fact]
    public void SelectedTheme_Set_RaisesPropertyChanged()
    {
        var vm   = new MainWindowViewModel(new GitDesktopClient(new MockGitExecutor()), IsolatedConfig());
        string? prop = null;
        vm.PropertyChanged += (_, e) => prop = e.PropertyName;

        vm.SelectedTheme = "Nord";

        Assert.Equal(nameof(MainWindowViewModel.SelectedTheme), prop);
    }

    [Fact]
    public void FontSize_Set_RaisesPropertyChanged()
    {
        var vm   = new MainWindowViewModel(new GitDesktopClient(new MockGitExecutor()), IsolatedConfig());
        string? prop = null;
        vm.PropertyChanged += (_, e) => prop = e.PropertyName;

        vm.FontSize = 16.0;

        Assert.Equal(nameof(MainWindowViewModel.FontSize), prop);
    }

    [Fact]
    public void AvailableThemes_ContainsFiveEntries()
    {
        var vm = new MainWindowViewModel(new GitDesktopClient(new MockGitExecutor()), IsolatedConfig());

        Assert.Equal(5, vm.AvailableThemes.Count);
    }
}
