using GitDesktop.App.Services;

namespace GitDesktop.App.Tests;

/// <summary>
/// Unit tests for <see cref="ThemeManager"/>.
/// </summary>
public class ThemeManagerTests
{
    [Fact]
    public void ThemeNames_ContainsFiveEntries()
    {
        Assert.Equal(5, ThemeManager.ThemeNames.Count);
    }

    [Fact]
    public void ThemeNames_ContainsAllExpectedNames()
    {
        Assert.Contains("Dark",           ThemeManager.ThemeNames);
        Assert.Contains("Light",          ThemeManager.ThemeNames);
        Assert.Contains("Monokai",        ThemeManager.ThemeNames);
        Assert.Contains("Solarized Dark", ThemeManager.ThemeNames);
        Assert.Contains("Nord",           ThemeManager.ThemeNames);
    }

    [Fact]
    public void ThemeNames_FirstEntryIsDark()
    {
        Assert.Equal("Dark", ThemeManager.ThemeNames[0]);
    }

    [Fact]
    public void GetTheme_Dark_ReturnsDarkTheme()
    {
        var theme = ThemeManager.GetTheme("Dark");
        Assert.Equal(ThemeManager.DarkTheme, theme);
    }

    [Fact]
    public void GetTheme_Light_ReturnsLightTheme()
    {
        var theme = ThemeManager.GetTheme("Light");
        Assert.Equal(ThemeManager.LightTheme, theme);
    }

    [Fact]
    public void GetTheme_Monokai_ReturnsMonokaiTheme()
    {
        var theme = ThemeManager.GetTheme("Monokai");
        Assert.Equal(ThemeManager.MonokaiTheme, theme);
    }

    [Fact]
    public void GetTheme_SolarizedDark_ReturnsSolarizedDarkTheme()
    {
        var theme = ThemeManager.GetTheme("Solarized Dark");
        Assert.Equal(ThemeManager.SolarizedDarkTheme, theme);
    }

    [Fact]
    public void GetTheme_Nord_ReturnsNordTheme()
    {
        var theme = ThemeManager.GetTheme("Nord");
        Assert.Equal(ThemeManager.NordTheme, theme);
    }

    [Fact]
    public void GetTheme_UnknownName_ReturnsDarkTheme()
    {
        var theme = ThemeManager.GetTheme("unknown-theme");
        Assert.Equal(ThemeManager.DarkTheme, theme);
    }

    [Fact]
    public void AllThemes_HaveDifferentBackgroundColors()
    {
        var backgrounds = new[]
        {
            ThemeManager.DarkTheme.WindowBackground,
            ThemeManager.LightTheme.WindowBackground,
            ThemeManager.MonokaiTheme.WindowBackground,
            ThemeManager.SolarizedDarkTheme.WindowBackground,
            ThemeManager.NordTheme.WindowBackground,
        };

        // Each theme should have a distinct background colour.
        Assert.Equal(backgrounds.Distinct().Count(), backgrounds.Length);
    }

    [Fact]
    public void AllThemes_HaveContrastingAddedAndRemovedColors()
    {
        foreach (var name in ThemeManager.ThemeNames)
        {
            var t = ThemeManager.GetTheme(name);
            Assert.NotEqual(t.AddedBackground, t.RemovedBackground);
            Assert.NotEqual(t.AddedForeground, t.RemovedForeground);
        }
    }

    [Fact]
    public void AllThemes_DefineSyntaxColors()
    {
        foreach (var name in ThemeManager.ThemeNames)
        {
            var t = ThemeManager.GetTheme(name);
            Assert.NotEqual(default, t.SyntaxComment);
            Assert.NotEqual(default, t.SyntaxKeyword);
            Assert.NotEqual(default, t.SyntaxString);
        }
    }
}
