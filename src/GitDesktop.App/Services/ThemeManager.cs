using Avalonia.Media;

namespace GitDesktop.App.Services;

/// <summary>
/// Defines a complete colour theme for the GitDesktop UI.
/// All colours are expressed as <see cref="Color"/> values so they can be
/// assigned directly to Avalonia <see cref="SolidColorBrush"/> resources.
/// </summary>
public sealed class ThemeColors
{
    public Color WindowBackground    { get; init; }
    public Color SidebarBackground   { get; init; }
    public Color PanelBackground     { get; init; }
    public Color ContentBackground   { get; init; }
    public Color ToolbarBackground   { get; init; }
    public Color StatusBarBackground { get; init; }

    public Color PrimaryText   { get; init; }
    public Color SecondaryText { get; init; }
    public Color AccentColor   { get; init; }
    public Color BranchColor   { get; init; }

    public Color AddedBackground   { get; init; }
    public Color AddedForeground   { get; init; }
    public Color RemovedBackground { get; init; }
    public Color RemovedForeground { get; init; }
    public Color HunkBackground    { get; init; }
    public Color HunkForeground    { get; init; }
    public Color DiffContextForeground { get; init; }

    public Color ButtonBackground  { get; init; }
    public Color ButtonForeground  { get; init; }
    public Color BorderColor       { get; init; }
    public Color SyntaxComment     { get; init; }
    public Color SyntaxKeyword     { get; init; }
    public Color SyntaxString      { get; init; }
}

/// <summary>
/// Provides the five built-in colour themes and helpers to apply the active
/// theme to the running Avalonia application.
/// </summary>
public static class ThemeManager
{
    /// <summary>Ordered list of available theme names.</summary>
    public static readonly IReadOnlyList<string> ThemeNames =
        ["Dark", "Light", "Monokai", "Solarized Dark", "Nord"];

    /// <summary>Returns the <see cref="ThemeColors"/> for the given theme name.</summary>
    public static ThemeColors GetTheme(string name) => name switch
    {
        "Light"         => LightTheme,
        "Monokai"       => MonokaiTheme,
        "Solarized Dark" => SolarizedDarkTheme,
        "Nord"          => NordTheme,
        _               => DarkTheme,           // "Dark" is the default
    };

    // ── 1 · Dark (VS Code–inspired) ──────────────────────────────────────────
    public static readonly ThemeColors DarkTheme = new()
    {
        WindowBackground    = Color.Parse("#1e1e1e"),
        SidebarBackground   = Color.Parse("#252526"),
        PanelBackground     = Color.Parse("#252526"),
        ContentBackground   = Color.Parse("#1e1e1e"),
        ToolbarBackground   = Color.Parse("#2d2d2d"),
        StatusBarBackground = Color.Parse("#1e1e1e"),

        PrimaryText   = Color.Parse("#d4d4d4"),
        SecondaryText = Color.Parse("#888888"),
        AccentColor   = Color.Parse("#569cd6"),
        BranchColor   = Color.Parse("#4ec9b0"),

        AddedBackground   = Color.Parse("#1a3a1a"),
        AddedForeground   = Color.Parse("#b5e7a0"),
        RemovedBackground = Color.Parse("#3a1a1a"),
        RemovedForeground = Color.Parse("#f48771"),
        HunkBackground    = Color.Parse("#1a2a3a"),
        HunkForeground    = Color.Parse("#9cdcfe"),
        DiffContextForeground = Color.Parse("#888888"),

        ButtonBackground  = Color.Parse("#3c3c3c"),
        ButtonForeground  = Color.Parse("#d4d4d4"),
        BorderColor       = Color.Parse("#3c3c3c"),
        SyntaxComment     = Color.Parse("#6A9955"),
        SyntaxKeyword     = Color.Parse("#C586C0"),
        SyntaxString      = Color.Parse("#CE9178"),
    };

    // ── 2 · Light ────────────────────────────────────────────────────────────
    public static readonly ThemeColors LightTheme = new()
    {
        WindowBackground    = Color.Parse("#ffffff"),
        SidebarBackground   = Color.Parse("#f3f3f3"),
        PanelBackground     = Color.Parse("#f3f3f3"),
        ContentBackground   = Color.Parse("#ffffff"),
        ToolbarBackground   = Color.Parse("#e8e8e8"),
        StatusBarBackground = Color.Parse("#f3f3f3"),

        PrimaryText   = Color.Parse("#1e1e1e"),
        SecondaryText = Color.Parse("#555555"),
        AccentColor   = Color.Parse("#005fb8"),
        BranchColor   = Color.Parse("#007070"),

        AddedBackground   = Color.Parse("#ccffcc"),
        AddedForeground   = Color.Parse("#007000"),
        RemovedBackground = Color.Parse("#ffcccc"),
        RemovedForeground = Color.Parse("#c00000"),
        HunkBackground    = Color.Parse("#ddeeff"),
        HunkForeground    = Color.Parse("#003c8a"),
        DiffContextForeground = Color.Parse("#555555"),

        ButtonBackground  = Color.Parse("#dddddd"),
        ButtonForeground  = Color.Parse("#1e1e1e"),
        BorderColor       = Color.Parse("#cccccc"),
        SyntaxComment     = Color.Parse("#008000"),
        SyntaxKeyword     = Color.Parse("#0000FF"),
        SyntaxString      = Color.Parse("#A31515"),
    };

    // ── 3 · Monokai ───────────────────────────────────────────────────────────
    public static readonly ThemeColors MonokaiTheme = new()
    {
        WindowBackground    = Color.Parse("#272822"),
        SidebarBackground   = Color.Parse("#1e1f1c"),
        PanelBackground     = Color.Parse("#2d2e27"),
        ContentBackground   = Color.Parse("#272822"),
        ToolbarBackground   = Color.Parse("#3e3d32"),
        StatusBarBackground = Color.Parse("#1e1f1c"),

        PrimaryText   = Color.Parse("#f8f8f2"),
        SecondaryText = Color.Parse("#75715e"),
        AccentColor   = Color.Parse("#66d9e8"),
        BranchColor   = Color.Parse("#a6e22e"),

        AddedBackground   = Color.Parse("#1d3a1a"),
        AddedForeground   = Color.Parse("#a6e22e"),
        RemovedBackground = Color.Parse("#3a1a1a"),
        RemovedForeground = Color.Parse("#f92672"),
        HunkBackground    = Color.Parse("#1a2c3a"),
        HunkForeground    = Color.Parse("#66d9e8"),
        DiffContextForeground = Color.Parse("#75715e"),

        ButtonBackground  = Color.Parse("#3e3d32"),
        ButtonForeground  = Color.Parse("#f8f8f2"),
        BorderColor       = Color.Parse("#49483e"),
        SyntaxComment     = Color.Parse("#75715e"),
        SyntaxKeyword     = Color.Parse("#f92672"),
        SyntaxString      = Color.Parse("#e6db74"),
    };

    // ── 4 · Solarized Dark ───────────────────────────────────────────────────
    public static readonly ThemeColors SolarizedDarkTheme = new()
    {
        WindowBackground    = Color.Parse("#002b36"),
        SidebarBackground   = Color.Parse("#073642"),
        PanelBackground     = Color.Parse("#073642"),
        ContentBackground   = Color.Parse("#002b36"),
        ToolbarBackground   = Color.Parse("#073642"),
        StatusBarBackground = Color.Parse("#002b36"),

        PrimaryText   = Color.Parse("#839496"),
        SecondaryText = Color.Parse("#586e75"),
        AccentColor   = Color.Parse("#268bd2"),
        BranchColor   = Color.Parse("#2aa198"),

        AddedBackground   = Color.Parse("#003d1f"),
        AddedForeground   = Color.Parse("#859900"),
        RemovedBackground = Color.Parse("#3d0000"),
        RemovedForeground = Color.Parse("#dc322f"),
        HunkBackground    = Color.Parse("#00203d"),
        HunkForeground    = Color.Parse("#268bd2"),
        DiffContextForeground = Color.Parse("#586e75"),

        ButtonBackground  = Color.Parse("#073642"),
        ButtonForeground  = Color.Parse("#839496"),
        BorderColor       = Color.Parse("#586e75"),
        SyntaxComment     = Color.Parse("#586e75"),
        SyntaxKeyword     = Color.Parse("#859900"),
        SyntaxString      = Color.Parse("#2aa198"),
    };

    // ── 5 · Nord ──────────────────────────────────────────────────────────────
    public static readonly ThemeColors NordTheme = new()
    {
        WindowBackground    = Color.Parse("#2e3440"),
        SidebarBackground   = Color.Parse("#3b4252"),
        PanelBackground     = Color.Parse("#3b4252"),
        ContentBackground   = Color.Parse("#2e3440"),
        ToolbarBackground   = Color.Parse("#434c5e"),
        StatusBarBackground = Color.Parse("#2e3440"),

        PrimaryText   = Color.Parse("#eceff4"),
        SecondaryText = Color.Parse("#4c566a"),
        AccentColor   = Color.Parse("#88c0d0"),
        BranchColor   = Color.Parse("#8fbcbb"),

        AddedBackground   = Color.Parse("#1a3a2a"),
        AddedForeground   = Color.Parse("#a3be8c"),
        RemovedBackground = Color.Parse("#3a1e28"),
        RemovedForeground = Color.Parse("#bf616a"),
        HunkBackground    = Color.Parse("#1e2d3a"),
        HunkForeground    = Color.Parse("#88c0d0"),
        DiffContextForeground = Color.Parse("#4c566a"),

        ButtonBackground  = Color.Parse("#4c566a"),
        ButtonForeground  = Color.Parse("#eceff4"),
        BorderColor       = Color.Parse("#4c566a"),
        SyntaxComment     = Color.Parse("#616e88"),
        SyntaxKeyword     = Color.Parse("#81a1c1"),
        SyntaxString      = Color.Parse("#a3be8c"),
    };

    /// <summary>
    /// Applies the given theme to <see cref="Avalonia.Application.Current"/>.
    /// Must be called on the UI thread.
    /// </summary>
    public static void ApplyTheme(string themeName)
    {
        var app = Avalonia.Application.Current;
        if (app == null) return;

        var t = GetTheme(themeName);
        var res = app.Resources;

        res["ThemeWindowBackground"]    = new SolidColorBrush(t.WindowBackground);
        res["ThemeSidebarBackground"]   = new SolidColorBrush(t.SidebarBackground);
        res["ThemePanelBackground"]     = new SolidColorBrush(t.PanelBackground);
        res["ThemeContentBackground"]   = new SolidColorBrush(t.ContentBackground);
        res["ThemeToolbarBackground"]   = new SolidColorBrush(t.ToolbarBackground);
        res["ThemeStatusBarBackground"] = new SolidColorBrush(t.StatusBarBackground);

        res["ThemePrimaryText"]   = new SolidColorBrush(t.PrimaryText);
        res["ThemeSecondaryText"] = new SolidColorBrush(t.SecondaryText);
        res["ThemeAccentColor"]   = new SolidColorBrush(t.AccentColor);
        res["ThemeBranchColor"]   = new SolidColorBrush(t.BranchColor);

        res["ThemeAddedBackground"]   = new SolidColorBrush(t.AddedBackground);
        res["ThemeAddedForeground"]   = new SolidColorBrush(t.AddedForeground);
        res["ThemeRemovedBackground"] = new SolidColorBrush(t.RemovedBackground);
        res["ThemeRemovedForeground"] = new SolidColorBrush(t.RemovedForeground);
        res["ThemeHunkBackground"]    = new SolidColorBrush(t.HunkBackground);
        res["ThemeHunkForeground"]    = new SolidColorBrush(t.HunkForeground);
        res["ThemeDiffContextForeground"] = new SolidColorBrush(t.DiffContextForeground);

        res["ThemeButtonBackground"]  = new SolidColorBrush(t.ButtonBackground);
        res["ThemeButtonForeground"]  = new SolidColorBrush(t.ButtonForeground);
        res["ThemeBorderColor"]       = new SolidColorBrush(t.BorderColor);
        res["ThemeSyntaxComment"]     = new SolidColorBrush(t.SyntaxComment);
        res["ThemeSyntaxKeyword"]     = new SolidColorBrush(t.SyntaxKeyword);
        res["ThemeSyntaxString"]      = new SolidColorBrush(t.SyntaxString);
    }
}
