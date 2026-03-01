using GitDesktop.App.Models;
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
    public async Task OpenRepositoryAsync_SamePath_SwitchesToExistingTabNoDuplicate()
    {
        var mock = ValidRepoMock();
        var vm   = new MainWindowViewModel(new GitDesktopClient(mock), IsolatedConfig());
        vm.RepoPath = "/repo-a";
        await vm.OpenRepositoryAsync();

        // Opening the same path again should not add a second tab.
        vm.RepoPath = "/repo-a";
        await vm.OpenRepositoryAsync();

        Assert.Single(vm.Tabs);
        Assert.Equal("/repo-a", vm.SelectedTab!.RepoPath);
    }

    [Fact]
    public async Task OpenRepositoryAsync_WithCustomName_UsesCustomName()
    {
        var mock = ValidRepoMock();
        var vm   = new MainWindowViewModel(new GitDesktopClient(mock), IsolatedConfig());
        vm.RepoPath  = "/repo-a";
        vm.CustomName = "My Awesome Repo";

        await vm.OpenRepositoryAsync();

        Assert.Equal("My Awesome Repo", vm.SelectedTab!.Name);
        // CustomName should be cleared after opening.
        Assert.Equal(string.Empty, vm.CustomName);
    }

    [Fact]
    public async Task OpenRepositoryAsync_NoCustomName_DerivesNameFromPath()
    {
        var mock = ValidRepoMock();
        var vm   = new MainWindowViewModel(new GitDesktopClient(mock), IsolatedConfig());
        vm.RepoPath = "/projects/my-project";

        await vm.OpenRepositoryAsync();

        Assert.Equal("my-project", vm.SelectedTab!.Name);
    }

    [Fact]
    public async Task SelectedTab_Changed_RaisesShowCommandPropertyChangedNotifications()
    {
        var mock = new MockGitExecutor();
        // Two repos
        mock.EnqueueSuccess(".git\nfalse\nmain"); mock.EnqueueSuccess("git version 2.40.0");
        mock.EnqueueSuccess("# branch.head main\n");
        mock.EnqueueSuccess("main|abc1234|true||0|0\n"); mock.EnqueueSuccess("");
        mock.EnqueueSuccess(".git\nfalse\ndev"); mock.EnqueueSuccess("git version 2.40.0");
        mock.EnqueueSuccess("# branch.head dev\n");
        mock.EnqueueSuccess("dev|def5678|true||0|0\n"); mock.EnqueueSuccess("");

        var vm = new MainWindowViewModel(new GitDesktopClient(mock), IsolatedConfig());
        vm.RepoPath = "/repo-a"; await vm.OpenRepositoryAsync();
        vm.RepoPath = "/repo-b"; await vm.OpenRepositoryAsync();

        var notified = new HashSet<string>();
        vm.PropertyChanged += (_, e) => { if (e.PropertyName != null) notified.Add(e.PropertyName); };

        vm.SelectedTab = vm.Tabs[0];

        Assert.Contains(nameof(MainWindowViewModel.ShowStatusCommand),   notified);
        Assert.Contains(nameof(MainWindowViewModel.ShowBranchesCommand), notified);
        Assert.Contains(nameof(MainWindowViewModel.ShowHistoryCommand),  notified);
        Assert.Contains(nameof(MainWindowViewModel.ShowTagsCommand),     notified);
        Assert.Contains(nameof(MainWindowViewModel.ShowRemotesCommand),  notified);
        Assert.Contains(nameof(MainWindowViewModel.ShowStashCommand),    notified);
        Assert.Contains(nameof(MainWindowViewModel.ShowFilesCommand),    notified);
    }

    [Fact]
    public async Task SelectedTab_Changed_ShowCommandsReturnNonNull()
    {
        var mock = ValidRepoMock();
        var vm   = new MainWindowViewModel(new GitDesktopClient(mock), IsolatedConfig());
        vm.RepoPath = "/repo-a";
        await vm.OpenRepositoryAsync();

        // With a tab selected all show-commands should be non-null.
        Assert.NotNull(vm.ShowStatusCommand);
        Assert.NotNull(vm.ShowBranchesCommand);
        Assert.NotNull(vm.ShowHistoryCommand);
        Assert.NotNull(vm.ShowTagsCommand);
        Assert.NotNull(vm.ShowRemotesCommand);
        Assert.NotNull(vm.ShowStashCommand);
        Assert.NotNull(vm.ShowFilesCommand);
    }

    [Fact]
    public async Task StartupAsync_RestoresOpenTabs()
    {
        var cfgPath = Path.Combine(_tempDir, $"cfg_{Guid.NewGuid():N}.json");
        var svc     = new AppConfigService(cfgPath);

        // Simulate a previous session: save two open paths to config.
        var cfg = new AppConfig
        {
            OpenRepositoryPaths = ["/repo-a", "/repo-b"],
        };
        await svc.SaveAsync(cfg);

        // Build a mock that answers OpenAsync + LoadAsync for both repos.
        var mock = new MockGitExecutor();
        // repo-a: OpenAsync + status + branches + history
        mock.EnqueueSuccess(".git\nfalse\nmain"); mock.EnqueueSuccess("git version 2.40.0");
        mock.EnqueueSuccess("# branch.head main\n");
        mock.EnqueueSuccess("main|abc1234|true||0|0\n"); mock.EnqueueSuccess("");
        // repo-b: OpenAsync + status + branches + history
        mock.EnqueueSuccess(".git\nfalse\ndev"); mock.EnqueueSuccess("git version 2.40.0");
        mock.EnqueueSuccess("# branch.head dev\n");
        mock.EnqueueSuccess("dev|def5678|true||0|0\n"); mock.EnqueueSuccess("");

        var vm = new MainWindowViewModel(new GitDesktopClient(mock), svc);
        await vm.StartupAsync();

        Assert.Equal(2, vm.Tabs.Count);
    }

    [Fact]
    public async Task StartupAsync_RestoresSelectedTab()
    {
        var cfgPath = Path.Combine(_tempDir, $"cfg_{Guid.NewGuid():N}.json");
        var svc     = new AppConfigService(cfgPath);

        var cfg = new AppConfig
        {
            OpenRepositoryPaths   = ["/repo-a", "/repo-b"],
            SelectedRepositoryPath = "/repo-b",
        };
        await svc.SaveAsync(cfg);

        var mock = new MockGitExecutor();
        mock.EnqueueSuccess(".git\nfalse\nmain"); mock.EnqueueSuccess("git version 2.40.0");
        mock.EnqueueSuccess("# branch.head main\n");
        mock.EnqueueSuccess("main|abc1234|true||0|0\n"); mock.EnqueueSuccess("");
        mock.EnqueueSuccess(".git\nfalse\ndev"); mock.EnqueueSuccess("git version 2.40.0");
        mock.EnqueueSuccess("# branch.head dev\n");
        mock.EnqueueSuccess("dev|def5678|true||0|0\n"); mock.EnqueueSuccess("");

        var vm = new MainWindowViewModel(new GitDesktopClient(mock), svc);
        await vm.StartupAsync();

        Assert.Equal("/repo-b", vm.SelectedTab!.RepoPath);
    }

    [Fact]
    public async Task SaveTabNameCommand_UpdatesTabNameAndConfig()
    {
        var cfgPath = Path.Combine(_tempDir, $"cfg_{Guid.NewGuid():N}.json");
        var svc     = new AppConfigService(cfgPath);

        var mock = ValidRepoMock();
        var vm   = new MainWindowViewModel(new GitDesktopClient(mock), svc);
        vm.RepoPath = "/repo-a";
        await vm.OpenRepositoryAsync();

        vm.SelectedTabName = "Renamed Tab";
        await vm.SaveTabNameAsync();

        Assert.Equal("Renamed Tab", vm.SelectedTab!.Name);

        // Verify the name is persisted to config.
        var loaded = await svc.LoadAsync();
        Assert.Equal("Renamed Tab", loaded.Repositories.First(r => r.Path.EndsWith("repo-a")).Name);
    }
}

