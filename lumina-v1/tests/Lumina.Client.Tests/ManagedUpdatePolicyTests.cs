using Lumina.Client.Services;
using Lumina.Core;
using Xunit;

namespace Lumina.Client.Tests;

public sealed class ManagedUpdatePolicyTests
{
    [Fact]
    public void Local_Content_Is_Never_Auto_Updated()
    {
        var local = ManagedContentRecord.Local("manual.jar", ContentKind.Mod);
        Assert.False(ManagedContentUpdatePolicy.IsUpdateAvailable(local, "new-version"));
    }

    [Fact]
    public void Same_Modrinth_Version_Is_Current()
    {
        var record = new ManagedContentRecord { ProjectId = "project", VersionId = "v1", FileName = "mod.jar", Kind = ContentKind.Mod };
        Assert.False(ManagedContentUpdatePolicy.IsUpdateAvailable(record, "v1"));
    }

    [Fact]
    public void Different_Modrinth_Version_Is_An_Update()
    {
        var record = new ManagedContentRecord { ProjectId = "project", VersionId = "v1", FileName = "mod.jar", Kind = ContentKind.Mod };
        Assert.True(ManagedContentUpdatePolicy.IsUpdateAvailable(record, "v2"));
    }
}
