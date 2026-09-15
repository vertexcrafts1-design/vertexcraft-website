using Xunit;

namespace Lumina.Client.Tests;

public sealed class ThemeContrastTests
{
    private static string Theme => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ConceptBUnderTest.xaml"));

    [Fact]
    public void ComboBox_Has_Custom_Dark_Popup_And_Item_States()
    {
        var xaml = Theme;

        Assert.Contains("TargetType=\"ComboBoxItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<ControlTemplate TargetType=\"ComboBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Popup", xaml, StringComparison.Ordinal);
        Assert.Contains("#0E131D", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#1B2434", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#2A2146", xaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ComboBox_Items_Explicitly_Use_Bright_Text()
    {
        var xaml = Theme;

        Assert.Contains("Foreground=\"#F4F6FB\"", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IsSelected", xaml, StringComparison.Ordinal);
        Assert.Contains("IsMouseOver", xaml, StringComparison.Ordinal);
    }
}
