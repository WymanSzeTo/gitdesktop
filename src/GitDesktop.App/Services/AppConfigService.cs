using System.Text.Json;
using System.Text.Json.Serialization;
using GitDesktop.App.Models;

namespace GitDesktop.App.Services;

/// <summary>
/// Loads and saves <see cref="AppConfig"/> as JSON.
/// The file is stored in the user's application-data folder:
/// <c>%APPDATA%\GitDesktop\config.json</c> on Windows, or
/// <c>~/.config/GitDesktop/config.json</c> on Linux / macOS.
/// An alternative path can be supplied via the constructor for testing.
/// </summary>
public sealed class AppConfigService
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    private readonly string? _overridePath;

    /// <summary>
    /// Creates an <see cref="AppConfigService"/> that uses the default OS path.
    /// </summary>
    public AppConfigService() { }

    /// <summary>
    /// Creates an <see cref="AppConfigService"/> with an explicit config file path.
    /// Intended for testing.
    /// </summary>
    public AppConfigService(string configFilePath) => _overridePath = configFilePath;

    /// <summary>Returns the full path to the configuration file.</summary>
    public string ConfigFilePath => _overridePath ?? DefaultConfigFilePath;

    /// <summary>Returns the default OS config file path.</summary>
    public static string DefaultConfigFilePath
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "GitDesktop", "config.json");
        }
    }

    /// <summary>
    /// Loads the configuration from disk. If the file does not exist, a default
    /// <see cref="AppConfig"/> instance is returned.
    /// </summary>
    public async Task<AppConfig> LoadAsync()
    {
        var path = ConfigFilePath;
        if (!File.Exists(path)) return new AppConfig();

        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<AppConfig>(stream, s_options)
                   ?? new AppConfig();
        }
        catch
        {
            // Corrupt / unreadable — start fresh.
            return new AppConfig();
        }
    }

    /// <summary>Persists <paramref name="config"/> to disk.</summary>
    public async Task SaveAsync(AppConfig config)
    {
        var path = ConfigFilePath;
        var dir  = Path.GetDirectoryName(path);
        if (dir is not null)
            Directory.CreateDirectory(dir);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, config, s_options);
    }

    /// <summary>
    /// Adds a repository entry to the config if it is not already present,
    /// then saves the updated config to disk.
    /// </summary>
    public async Task AddRepositoryAsync(AppConfig config, string repoPath, string? name = null)
    {
        var normalised = Path.GetFullPath(repoPath);
        if (config.Repositories.Any(r => Path.GetFullPath(r.Path) == normalised))
            return;

        config.Repositories.Add(new RepositoryEntry
        {
            Path = normalised,
            Name = name ?? Path.GetFileName(normalised.TrimEnd(Path.DirectorySeparatorChar)),
        });

        await SaveAsync(config);
    }

    /// <summary>
    /// Removes a repository entry by path and saves the updated config to disk.
    /// </summary>
    public async Task RemoveRepositoryAsync(AppConfig config, string repoPath)
    {
        var normalised = Path.GetFullPath(repoPath);
        config.Repositories.RemoveAll(r => Path.GetFullPath(r.Path) == normalised);
        await SaveAsync(config);
    }
}
