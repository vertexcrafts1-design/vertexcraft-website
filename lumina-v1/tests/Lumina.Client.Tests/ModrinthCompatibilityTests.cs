using Lumina.Client.Services;
using Lumina.Core;
using Xunit;

namespace Lumina.Client.Tests;

public sealed class ModrinthCompatibilityTests
{
    [Fact]
    public void Fabric_Instance_Rejects_Forge_Only_Mod()
    {
        var instance = new InstanceProfile { Version = "1.21.1", Loader = "Fabric", LoaderVersion = "0.16.14" };
        var candidate = new ModrinthVersionDescriptor(["1.21.1"], ["forge"], "release");

        var result = CompatibilityService.Check(instance, ContentKind.Mod, candidate);

        Assert.False(result.IsCompatible);
        Assert.Contains("Fabric", result.Reason);
    }

    [Fact]
    public void Fabric_Instance_Accepts_Fabric_Mod_For_Exact_Game_Version()
    {
        var instance = new InstanceProfile { Version = "1.21.1", Loader = "Fabric", LoaderVersion = "0.16.14" };
        var candidate = new ModrinthVersionDescriptor(["1.21.1"], ["fabric"], "release");

        var result = CompatibilityService.Check(instance, ContentKind.Mod, candidate);

        Assert.True(result.IsCompatible);
    }

    [Fact]
    public void Resource_Pack_Only_Requires_Game_Version()
    {
        var instance = new InstanceProfile { Version = "1.21.1", Loader = "NeoForge", LoaderVersion = "21.1.0" };
        var candidate = new ModrinthVersionDescriptor(["1.21.1"], [], "release");

        var result = CompatibilityService.Check(instance, ContentKind.ResourcePack, candidate);

        Assert.True(result.IsCompatible);
    }

    [Fact]
    public void Release_Is_Preferred_Over_Beta_And_Alpha()
    {
        var candidates = new[]
        {
            new ModrinthVersion { Id = "alpha", VersionType = "alpha" },
            new ModrinthVersion { Id = "beta", VersionType = "beta" },
            new ModrinthVersion { Id = "release", VersionType = "release" }
        };

        var selected = CompatibilityService.ChoosePreferred(candidates);

        Assert.Equal("release", selected?.Id);
    }

    [Fact]
    public void Alpha_Is_Not_Automatically_Selected()
    {
        var candidates = new[] { new ModrinthVersion { Id = "alpha", VersionType = "alpha" } };
        Assert.Null(CompatibilityService.ChoosePreferred(candidates));
    }
}
