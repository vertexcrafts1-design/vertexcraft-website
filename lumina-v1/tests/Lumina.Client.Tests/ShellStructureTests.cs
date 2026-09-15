using Xunit;

namespace Lumina.Client.Tests;

public sealed class ShellStructureTests
{
    private static string Shell => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "MainWindowUnderTest.xaml"));

    [Fact]
    public void MainWindow_Contains_ConceptB_Shell()
    {
        var xaml = Shell;
        Assert.Contains("Width=\"236\"", xaml, StringComparison.Ordinal);
        Assert.Contains("EXPLORE", xaml, StringComparison.Ordinal);
        Assert.Contains("CREATE", xaml, StringComparison.Ordinal);
        Assert.Contains("BELONG", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ActiveInstanceCombo\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Compact client rail", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Sidebar_Uses_Icon_And_Text_Labels_In_Approved_Order()
    {
        var xaml = Shell;
        var labels = new[] { ">Home<", ">Discover<", ">Instanzen<", ">Bibliothek<", ">Downloads<", ">Einstellungen<", ">Account<" };
        var cursor = -1;
        foreach (var label in labels)
        {
            var next = xaml.IndexOf(label, cursor + 1, StringComparison.Ordinal);
            Assert.True(next > cursor, $"Missing or out-of-order sidebar label {label}");
            cursor = next;
        }
    }
}
