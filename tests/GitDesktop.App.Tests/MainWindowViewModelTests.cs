using GitDesktop.App.Services;
using GitDesktop.App.ViewModels;
using GitDesktop.Core;
using GitDesktop.Core.Execution;

namespace GitDesktop.App.Tests;

/// <summary>
/// Unit tests for <see cref="MainWindowViewModel"/>.
/// </summary>
public class MainWindowViewModelTests : IDisposable
{
    private readonly string _tempDir;

    public MainWindowViewModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"GitDesktop_MainWin_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private AppConfigService IsolatedConfig() =>
        new(Path.Combine(_tempDir, $"cfg_{Guid.NewGuid():N}.json"));

    [Fact]
    public void Constructor_InitialState_TitleIsGitDesktop()
    {
        var mock = new MockGitExecutor();
        var vm = new MainWindowViewModel(new GitDesktopClient(mock), IsolatedConfig());

        Assert.Equal("GitDesktop", vm.Title);
        Assert.Null(vm.CurrentView);
        Assert.Null(vm.ErrorMessage);
        Assert.Null(vm.StatusVM);
        Assert.Null(vm.BranchesVM);
        Assert.Null(vm.HistoryVM);
    }

    [Fact]
    public void RepoPath_Set_RaisesPropertyChanged()
    {
        var mock = new MockGitExecutor();
        var vm = new MainWindowViewModel(new GitDesktopClient(mock), IsolatedConfig());

        string? changedProperty = null;
        vm.PropertyChanged += (_, e) => changedProperty = e.PropertyName;

        vm.RepoPath = "/some/path";

        Assert.Equal(nameof(MainWindowViewModel.RepoPath), changedProperty);
    }

    [Fact]
    public async Task OpenRepositoryAsync_InvalidRepo_SetsErrorMessage()
    {
        var mock = new MockGitExecutor();
        // OpenAsync sends two git commands; make the first fail
        mock.EnqueueFailure("not a git repository", 128);

        var vm = new MainWindowViewModel(new GitDesktopClient(mock), IsolatedConfig());
        vm.RepoPath = "/tmp/notarepo";

        await vm.OpenRepositoryAsync();

        Assert.NotNull(vm.ErrorMessage);
        Assert.Null(vm.StatusVM);
    }

    [Fact]
    public async Task OpenRepositoryAsync_ValidRepo_SetsTitle()
    {
        var mock = new MockGitExecutor();
        // Enqueue responses for OpenAsync (rev-parse + git version)
        mock.EnqueueSuccess(".git\nfalse\nmain");
        mock.EnqueueSuccess("git version 2.40.0");
        // Status response
        mock.EnqueueSuccess("# branch.head main\n");
        // Branch list response
        mock.EnqueueSuccess("main|abc1234|true||0|0\n");
        // History log response
        mock.EnqueueSuccess("");

        var vm = new MainWindowViewModel(new GitDesktopClient(mock), IsolatedConfig());
        vm.RepoPath = "/repo";

        await vm.OpenRepositoryAsync();

        Assert.Contains("/repo", vm.Title);
        Assert.NotNull(vm.StatusVM);
        Assert.NotNull(vm.BranchesVM);
        Assert.NotNull(vm.HistoryVM);
        Assert.NotNull(vm.TagsVM);
        Assert.NotNull(vm.RemotesVM);
        Assert.NotNull(vm.StashVM);
        Assert.IsType<StatusViewModel>(vm.CurrentView);
    }
}
