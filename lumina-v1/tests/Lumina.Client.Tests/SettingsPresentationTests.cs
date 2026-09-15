using Xunit;

namespace Lumina.Client.Tests;

public sealed class SettingsPresentationTests
{
    private static string MainWindow => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "MainWindowUnderTest.xaml"));

    [Fact]
    public void Settings_Has_Clear_Sections_And_Ram_Presets()
    {
        var xaml = MainWindow;
        foreach (var label in new[] { "Performance", "Game Window", "Java", "Safety", "Quick Connect" })
            Assert.Contains(label, xaml, StringComparison.OrdinalIgnoreCase);

        foreach (var preset in new[] { "4 GB", "6 GB", "8 GB", "12 GB", "16 GB" })
            Assert.Contains(preset, xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Settings_Uses_ConceptB_Controls()
    {
        var xaml = MainWindow;
        Assert.Contains("LuminaToggle", xaml, StringComparison.Ordinal);
        Assert.Contains("LuminaPrimaryButton", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"RamSlider\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"JavaPathText\"", xaml, StringComparison.Ordinal);
    }
}
