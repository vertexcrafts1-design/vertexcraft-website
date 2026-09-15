using Xunit;

namespace Lumina.Client.Tests;

public sealed class DiscoverPresentationTests
{
    private static string Shell => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "MainWindowUnderTest.xaml"));

    [Fact]
    public void Discover_Has_Storefront_Tabs_And_Instance_Filters()
    {
        var xaml = Shell;
        foreach (var text in new[] { "Featured", "Modpacks", "Mods", "Shaders", "Resource Packs" })
            Assert.Contains(text, xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DiscoverVersionFilter\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DiscoverLoaderFilter\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DiscoverSearch\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Discover_Cards_Expose_Visual_Project_And_Install_Action()
    {
        var xaml = Shell;
        Assert.Contains("IconUrl", xaml, StringComparison.Ordinal);
        Assert.Contains("DownloadsText", xaml, StringComparison.Ordinal);
        Assert.Contains("GalleryInstall_Click", xaml, StringComparison.Ordinal);
        Assert.Contains("Installieren", xaml, StringComparison.Ordinal);
    }
}
