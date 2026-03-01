namespace GitDesktop.App.Models;

/// <summary>
/// Represents a single repository entry stored in the user configuration.
/// </summary>
public sealed class RepositoryEntry
{
    /// <summary>Gets or sets the display name for the repository.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the local path to the repository.</summary>
    public string Path { get; set; } = string.Empty;
}

/// <summary>
/// Root user-configuration model. Persisted as JSON in the user's app-data folder.
/// </summary>
public sealed class AppConfig
{
    /// <summary>Gets or sets the list of known repositories.</summary>
    public List<RepositoryEntry> Repositories { get; set; } = [];

    /// <summary>Gets or sets the active colour theme name (e.g. "Dark", "Light", "Monokai", "Solarized Dark", "Nord").</summary>
    public string Theme { get; set; } = "Dark";

    /// <summary>Gets or sets the base font size used throughout the UI.</summary>
    public double FontSize { get; set; } = 13.0;
}
