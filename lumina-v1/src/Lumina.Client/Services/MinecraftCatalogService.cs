using CmlLib.Core;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ModLoaders.QuiltMC;
using Lumina.Core;

namespace Lumina.Client.Services;

public sealed class MinecraftCatalogService
{
    private readonly AppPaths _paths;
    private readonly HttpClient _http = new();

    public MinecraftCatalogService(AppPaths paths) => _paths = paths;

    public async Task<IReadOnlyList<MinecraftVersionChoice>> GetMinecraftVersionsAsync(
        bool includeSnapshots,
        CancellationToken cancellationToken = default)
    {
        var catalogPath = Path.Combine(_paths.Root, "catalog");
        var launcher = new MinecraftLauncher(new MinecraftPath(catalogPath));
        var versions = await launcher.GetAllVersionsAsync(cancellationToken);

        return versions
            .Where(v => string.Equals(v.Type, "release", StringComparison.OrdinalIgnoreCase)
                     || (includeSnapshots && string.Equals(v.Type, "snapshot", StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(v => v.ReleaseTime)
            .Select(v => new MinecraftVersionChoice(v.Name, v.Type ?? "unknown", v.ReleaseTime))
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetLoaderVersionsAsync(
        string minecraftVersion,
        string loader,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(minecraftVersion)) return [];

        try
        {
            switch (loader.Trim().ToLowerInvariant())
            {
                case "vanilla":
                    return ["Standard"];

                case "fabric":
                {
                    var installer = new FabricInstaller(_http);
                    var loaders = await installer.GetLoaders(minecraftVersion);
                    return loaders.Where(x => !string.IsNullOrWhiteSpace(x.Version))
                        .OrderByDescending(x => x.Stable)
                        .Select(x => x.Version!)
                        .Distinct()
                        .Take(25)
                        .ToList();
                }

                case "quilt":
                {
                    var installer = new QuiltInstaller(_http);
                    var loaders = await installer.GetLoaders(minecraftVersion);
                    return loaders.Where(x => !string.IsNullOrWhiteSpace(x.Version))
                        .OrderByDescending(x => x.Stable)
                        .Select(x => x.Version!)
                        .Distinct()
                        .Take(25)
                        .ToList();
                }

                case "forge":
                {
                    var launcher = new MinecraftLauncher(new MinecraftPath(Path.Combine(_paths.Root, "catalog-forge")));
                    var installer = new ForgeInstaller(launcher, _http);
                    var versions = await installer.GetForgeVersions(minecraftVersion);
                    return versions.Select(x => x.ForgeVersionName)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct()
                        .Take(40)
                        .ToList();
                }

                case "neoforge":
                {
                    var launcher = new MinecraftLauncher(new MinecraftPath(Path.Combine(_paths.Root, "catalog-neoforge")));
                    var installer = new NeoForgeInstaller(launcher);
                    var versions = await installer.GetForgeVersions(minecraftVersion);
                    return versions.Select(x => x.VersionName)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct()
                        .Take(40)
                        .ToList();
                }
            }
        }
        catch
        {
            // The UI shows an empty version list for unsupported loader/game-version pairs.
        }

        return [];
    }

    public static IReadOnlyList<string> Loaders { get; } = ["Vanilla", "Fabric", "Quilt", "Forge", "NeoForge"];
}
