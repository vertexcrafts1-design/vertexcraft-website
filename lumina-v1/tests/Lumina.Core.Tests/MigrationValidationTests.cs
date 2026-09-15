using Lumina.Core;
using Xunit;

namespace Lumina.Core.Tests;

public sealed class MigrationValidationTests
{
    [Fact]
    public async Task LatestRelease_Becomes_Concrete_Release()
    {
        var profile = new InstanceProfile { Name = "Old", Version = "latest-release", Loader = "vanilla" };
        var sut = new InstanceMigrationService();

        var result = await sut.MigrateAsync(
            profile,
            latestRelease: "1.21.11",
            latestSnapshot: "26w37a",
            knownVersions: ["1.21.11", "1.21.10"],
            getLoaderVersions: (_, _) => Task.FromResult<IReadOnlyList<string>>(["Standard"]));

        Assert.Equal("1.21.11", profile.Version);
        Assert.Equal("Vanilla", profile.Loader);
        Assert.Equal("Standard", profile.LoaderVersion);
        Assert.True(result.Changed);
        Assert.True(profile.IsValid);
    }

    [Fact]
    public async Task Unknown_Concrete_Version_Is_Invalid_Not_Rewritten()
    {
        var profile = new InstanceProfile { Version = "1.99.99", Loader = "Fabric", LoaderVersion = "0.17.2" };
        var sut = new InstanceMigrationService();

        await sut.MigrateAsync(
            profile,
            "1.21.11",
            "26w37a",
            ["1.21.11"],
            (_, _) => Task.FromResult<IReadOnlyList<string>>([]));

        Assert.Equal("1.99.99", profile.Version);
        Assert.False(profile.IsValid);
        Assert.Contains("Minecraft-Version", profile.InvalidReason);
    }

    [Fact]
    public async Task LatestSnapshot_Is_Migrated_When_Snapshots_Are_Allowed()
    {
        var profile = new InstanceProfile
        {
            Version = "latest-snapshot",
            Loader = "Quilt",
            LoaderVersion = "0.29.0",
            AllowSnapshots = true
        };
        var sut = new InstanceMigrationService();

        var result = await sut.MigrateAsync(
            profile,
            "1.21.11",
            "26w37a",
            ["1.21.11", "26w37a"],
            (_, _) => Task.FromResult<IReadOnlyList<string>>(["0.29.0"]));

        Assert.Equal("26w37a", profile.Version);
        Assert.True(profile.IsValid);
        Assert.True(result.Changed);
    }

    [Fact]
    public async Task LatestSnapshot_Remains_Invalid_When_Snapshots_Are_Not_Allowed()
    {
        var profile = new InstanceProfile { Version = "latest-snapshot", Loader = "Vanilla" };
        var sut = new InstanceMigrationService();

        await sut.MigrateAsync(
            profile,
            "1.21.11",
            "26w37a",
            ["1.21.11", "26w37a"],
            (_, _) => Task.FromResult<IReadOnlyList<string>>(["Standard"]));

        Assert.Equal("latest-snapshot", profile.Version);
        Assert.False(profile.IsValid);
    }

    [Theory]
    [InlineData("latest-release")]
    [InlineData("latest-snapshot")]
    public void Validation_Rejects_Pseudo_Versions(string version)
    {
        var profile = new InstanceProfile { Version = version, Loader = "Vanilla", LoaderVersion = "Standard" };
        var result = InstanceValidationService.Validate(profile, ["1.21.11"], ["Standard"]);

        Assert.False(result.IsValid);
        Assert.Contains("konkrete Minecraft-Version", result.Error);
    }
}
