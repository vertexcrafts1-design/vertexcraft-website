using Xunit;

namespace Lumina.Client.Tests;

public sealed class InstanceWizardPresentationTests
{
    private static string Wizard => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "InstanceWizardUnderTest.xaml"));

    [Fact]
    public void Wizard_Keeps_Five_Focused_Steps()
    {
        var xaml = Wizard;
        foreach (var text in new[] { "1 · Name", "2 · Minecraft", "3 · Loader", "4 · Version", "5 · Fertig" })
            Assert.Contains(text, xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Loader_Step_Shows_All_Client_Loaders_As_Visible_Cards()
    {
        var xaml = Wizard;
        foreach (var loader in new[] { "Vanilla", "Fabric", "Quilt", "Forge", "NeoForge" })
            Assert.Contains($">{loader}<", xaml, StringComparison.Ordinal);
        Assert.Contains("LoaderCard_Click", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Review_Step_Uses_Concrete_Values_And_No_Latest_Aliases()
    {
        var xaml = Wizard;
        Assert.Contains("Zusammenfassung", xaml, StringComparison.Ordinal);
        Assert.Contains("keine latest-* Platzhalter", xaml, StringComparison.Ordinal);
    }
}
