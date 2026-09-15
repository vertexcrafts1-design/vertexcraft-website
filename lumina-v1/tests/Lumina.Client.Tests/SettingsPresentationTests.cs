using Xunit;

namespace Lumina.Client.Tests;

public sealed class SettingsPresentationTests
{
    private static string MainWindow => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "MainWindowUnderTest.xaml"));
    private static string SettingsControls => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SettingsControlsUnderTest.xaml"));

    [Fact]
    public void Settings_Has_Clear_Sections()
    {
        var xaml = MainWindow;
        foreach (var label in new[] { "Performance", "Minecraft", "JAVA RUNTIME", "Safe Launch", "QUICK CONNECT" })
            Assert.Contains(label, xaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Ram_Has_Five_Obvious_Presets_And_Advanced_Slider()
    {
        var xaml = SettingsControls;
        foreach (var preset in new[] { "4 GB", "6 GB", "8 GB", "12 GB", "16 GB" })
            Assert.Contains(preset, xaml, StringComparison.Ordinal);
        foreach (var value in new[] { "ram:4096", "ram:6144", "ram:8192", "ram:12288", "ram:16384" })
            Assert.Contains(value, xaml, StringComparison.Ordinal);

        Assert.Contains("PART_Track", xaml, StringComparison.Ordinal);
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
