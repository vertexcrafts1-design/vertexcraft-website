using Xunit;

namespace Lumina.Client.Tests;

public sealed class DiscoverPresentationTests
{
    private static string Storefront => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DiscoverStorefrontUnderTest.xaml"));

    [Fact]
    public void Discover_Has_Storefront_Tabs_And_Instance_Filters()
    {
        var xaml = Storefront;
        foreach (var text in new[] { "Featured", "Modpacks", "Mods", "Shaders", "Resource Packs" })
            Assert.Contains(text, xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DiscoverVersionFilter\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DiscoverLoaderFilter\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"SearchBox\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Discover_Cards_Expose_Visual_Project_And_Install_Action()
    {
        var xaml = Storefront;
        Assert.Contains("IconUrl", xaml, StringComparison.Ordinal);
        Assert.Contains("DownloadsText", xaml, StringComparison.Ordinal);
        Assert.Contains("Install_Click", xaml, StringComparison.Ordinal);
        Assert.Contains("Installieren", xaml, StringComparison.Ordinal);
    }
}
