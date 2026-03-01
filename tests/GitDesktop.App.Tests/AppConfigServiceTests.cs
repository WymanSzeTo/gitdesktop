using GitDesktop.App.Models;
using GitDesktop.App.Services;

namespace GitDesktop.App.Tests;

/// <summary>
/// Unit tests for <see cref="AppConfigService"/>.
/// </summary>
public class AppConfigServiceTests : IDisposable
{
    private readonly string _tempDir;

    public AppConfigServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"GitDesktop_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private AppConfigService MakeSvc() =>
        new(Path.Combine(_tempDir, "config.json"));

    [Fact]
    public async Task LoadAsync_MissingFile_ReturnsDefaultConfig()
    {
        var svc    = MakeSvc();
        var config = await svc.LoadAsync();

        Assert.NotNull(config);
        Assert.Empty(config.Repositories);
        Assert.Equal("Dark", config.Theme);
        Assert.Equal(13.0, config.FontSize);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsData()
    {
        var svc = MakeSvc();
        var cfg = new AppConfig
        {
            Theme    = "Nord",
            FontSize = 16.0,
            Repositories =
            [
                new RepositoryEntry { Name = "my-project", Path = "/home/user/my-project" },
            ],
        };

        await svc.SaveAsync(cfg);
        var loaded = await svc.LoadAsync();

        Assert.Equal("Nord", loaded.Theme);
        Assert.Equal(16.0, loaded.FontSize);
        Assert.Single(loaded.Repositories);
        Assert.Equal("my-project", loaded.Repositories[0].Name);
        Assert.Equal("/home/user/my-project", loaded.Repositories[0].Path);
    }

    [Fact]
    public async Task AddRepositoryAsync_NewPath_AddsEntry()
    {
        var svc = MakeSvc();
        var cfg = new AppConfig();

        await svc.AddRepositoryAsync(cfg, "/home/user/repo-a", "repo-a");

        Assert.Single(cfg.Repositories);
        Assert.Equal("repo-a", cfg.Repositories[0].Name);
    }

    [Fact]
    public async Task AddRepositoryAsync_DuplicatePath_DoesNotAddTwice()
    {
        var svc = MakeSvc();
        var cfg = new AppConfig();

        await svc.AddRepositoryAsync(cfg, "/home/user/repo-a", "repo-a");
        await svc.AddRepositoryAsync(cfg, "/home/user/repo-a", "repo-a");

        Assert.Single(cfg.Repositories);
    }

    [Fact]
    public async Task RemoveRepositoryAsync_ExistingPath_RemovesEntry()
    {
        var svc = MakeSvc();
        var cfg = new AppConfig
        {
            Repositories =
            [
                new RepositoryEntry { Name = "repo-a", Path = "/home/user/repo-a" },
                new RepositoryEntry { Name = "repo-b", Path = "/home/user/repo-b" },
            ],
        };

        await svc.RemoveRepositoryAsync(cfg, "/home/user/repo-a");

        Assert.Single(cfg.Repositories);
        Assert.Equal("repo-b", cfg.Repositories[0].Name);
    }

    [Fact]
    public void ConfigFilePath_MatchesProvidedPath()
    {
        var expected = Path.Combine(_tempDir, "config.json");
        var svc      = new AppConfigService(expected);

        Assert.Equal(expected, svc.ConfigFilePath);
    }

    [Fact]
    public void DefaultConfigFilePath_ContainsGitDesktop()
    {
        Assert.Contains("GitDesktop", AppConfigService.DefaultConfigFilePath);
    }

    [Fact]
    public async Task UpdateRepositoryNameAsync_ExistingPath_UpdatesName()
    {
        var svc = MakeSvc();
        var cfg = new AppConfig
        {
            Repositories =
            [
                new RepositoryEntry { Name = "old-name", Path = "/home/user/repo-a" },
            ],
        };

        await svc.UpdateRepositoryNameAsync(cfg, "/home/user/repo-a", "new-name");

        Assert.Equal("new-name", cfg.Repositories[0].Name);

        // Verify persisted.
        var loaded = await svc.LoadAsync();
        Assert.Equal("new-name", loaded.Repositories[0].Name);
    }

    [Fact]
    public async Task UpdateRepositoryNameAsync_UnknownPath_IsNoOp()
    {
        var svc = MakeSvc();
        var cfg = new AppConfig
        {
            Repositories =
            [
                new RepositoryEntry { Name = "repo-a", Path = "/home/user/repo-a" },
            ],
        };

        // Should not throw or modify existing entries.
        await svc.UpdateRepositoryNameAsync(cfg, "/home/user/other", "new-name");

        Assert.Single(cfg.Repositories);
        Assert.Equal("repo-a", cfg.Repositories[0].Name);
    }

    [Fact]
    public async Task UpdateOpenSessionAsync_PersistsOpenPathsAndSelectedPath()
    {
        var svc = MakeSvc();
        var cfg = new AppConfig();

        await svc.UpdateOpenSessionAsync(cfg, ["/repo-a", "/repo-b"], "/repo-b");

        var loaded = await svc.LoadAsync();
        Assert.Equal(2, loaded.OpenRepositoryPaths.Count);
        Assert.Equal("/repo-b", loaded.SelectedRepositoryPath);
    }
}
