using Xunit;

namespace Lumina.Client.Tests;

public sealed class ConceptBThemeTests
{
    private static string Theme => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ConceptBUnderTest.xaml"));

    [Fact]
    public void ConceptB_Theme_Defines_Core_Resources()
    {
        var xaml = Theme;
        foreach (var key in new[]
        {
            "LuminaWindowBackground", "LuminaSidebarBackground", "LuminaSurface",
            "LuminaAccentBrush", "LuminaNavButton", "LuminaPrimaryButton",
            "LuminaComboBox", "LuminaToastCard", "LuminaModalCard"
        })
            Assert.Contains($"x:Key=\"{key}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void ConceptB_ComboBox_Uses_Dark_Popup_And_Bright_Text()
    {
        var xaml = Theme;
        Assert.Contains("PART_Popup", xaml, StringComparison.Ordinal);
        Assert.Contains("ComboBoxItem", xaml, StringComparison.Ordinal);
        Assert.Contains("#F4F6FB", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SystemColors.WindowBrush", xaml, StringComparison.Ordinal);
    }
}
