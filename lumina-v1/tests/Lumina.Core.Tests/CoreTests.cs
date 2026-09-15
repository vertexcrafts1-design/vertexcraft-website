using Lumina.Core;
using Xunit;

namespace Lumina.Core.Tests;

public sealed class CoreTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "lumina-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void SafeId_Normalizes_Name()
    {
        Assert.Equal("fabric-performance", InstanceService.SafeId("  Fabric Performance!! "));
    }

    [Fact]
    public void Create_Creates_Isolated_Content_Folders_And_Unique_Ids()
    {
        var paths = new AppPaths(_root);
        var service = new InstanceService(paths);
        var first = service.Create("Main", "1.21.1", "Fabric");
        var second = service.Create("Main", "1.21.1", "Fabric");

        Assert.Equal("main", first.Id);
        Assert.Equal("main-2", second.Id);
        Assert.True(Directory.Exists(paths.Mods(first.Id)));
        Assert.True(Directory.Exists(paths.ResourcePacks(first.Id)));
        Assert.True(Directory.Exists(paths.ShaderPacks(first.Id)));
    }

    [Fact]
    public void Content_Import_Toggle_And_Delete_Works()
    {
        var paths = new AppPaths(_root);
        var instances = new InstanceService(paths);
        var content = new ContentService(paths);
        var profile = instances.Create("Test", "1.21.1", "Fabric");
        var source = Path.Combine(_root, "example.jar");
        File.WriteAllText(source, "demo");

        var items = content.Import(profile.Id, ContentKind.Mod, [source]);
        Assert.Single(items);
        Assert.True(items[0].Enabled);

        var disabledPath = content.Toggle(items[0].Path);
        Assert.EndsWith(".disabled", disabledPath);
        Assert.False(content.Get(profile.Id, ContentKind.Mod)[0].Enabled);

        content.Delete(disabledPath);
        Assert.Empty(content.Get(profile.Id, ContentKind.Mod));
    }

    [Fact]
    public void Presets_Are_Distinct_And_Smart_Memory_Is_Bounded()
    {
        Assert.True(PresetService.Get("Quality").RamMb > PresetService.Get("Low Spec").RamMb);
        Assert.Equal(4096, PresetService.SmartRamMb(8UL * 1024 * 1024 * 1024));
        Assert.Equal(10240, PresetService.SmartRamMb(64UL * 1024 * 1024 * 1024));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
