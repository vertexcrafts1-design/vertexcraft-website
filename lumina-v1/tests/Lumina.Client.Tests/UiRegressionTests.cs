using Xunit;

namespace Lumina.Client.Tests;

public sealed class UiRegressionTests
{
    private static string MainWindow => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "MainWindowUnderTest.xaml"));
    private static string Theme => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ConceptBUnderTest.xaml"));
    private static string Adapter => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "MessageBoxAdapterUnderTest.cs"));

    [Fact]
    public void Shell_Keeps_ConceptB_Structure()
    {
        var xaml = MainWindow;
        Assert.Contains("<ColumnDefinition Width=\"236\"", xaml, StringComparison.Ordinal);
        foreach (var label in new[] { "Home", "Discover", "Instanzen", "Bibliothek", "Downloads", "Einstellungen", "Account" })
            Assert.Contains(label, xaml, StringComparison.Ordinal);
        Assert.Contains("EXPLORE", xaml, StringComparison.Ordinal);
        Assert.Contains("CREATE", xaml, StringComparison.Ordinal);
        Assert.Contains("BELONG", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ActiveInstanceCombo\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Compact client rail", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Dark_Controls_And_Hero_Fallback_Remain_Explicit()
    {
        var xaml = Theme;
        Assert.Contains("x:Key=\"LuminaHeroFallback\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"LuminaComboBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"LuminaComboBoxItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PART_Popup", xaml, StringComparison.Ordinal);
        Assert.Contains("#F4F6FB", xaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Account_And_Feedback_Do_Not_Fall_Back_To_Native_Password_Or_Alerts()
    {
        Assert.DoesNotContain("PasswordBox", MainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Windows.MessageBox.Show", Adapter, StringComparison.Ordinal);
        Assert.Contains("main.ShowFeedback", Adapter, StringComparison.Ordinal);
    }
}
