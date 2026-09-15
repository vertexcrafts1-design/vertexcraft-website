using Lumina.Core;

namespace Lumina.Client.Services;

public static class CompatibilityService
{
    public static CompatibilityResult Check(
        InstanceProfile instance,
        ContentKind kind,
        ModrinthVersionDescriptor candidate)
    {
        if (!candidate.GameVersions.Contains(instance.Version, StringComparer.OrdinalIgnoreCase))
            return CompatibilityResult.Incompatible($"Nicht für Minecraft {instance.Version} verfügbar.");

        if (kind != ContentKind.Mod)
            return CompatibilityResult.Compatible();

        if (instance.Loader.Equals("Vanilla", StringComparison.OrdinalIgnoreCase))
            return CompatibilityResult.Incompatible("Vanilla unterstützt keine normalen Modloader-Mods.");

        var expected = ModrinthService.LoaderForModrinth(instance.Loader);
        if (!candidate.Loaders.Contains(expected, StringComparer.OrdinalIgnoreCase))
            return CompatibilityResult.Incompatible($"Diese Version unterstützt nicht den Loader {instance.Loader}.");

        return CompatibilityResult.Compatible();
    }

    public static ModrinthVersion? ChoosePreferred(IEnumerable<ModrinthVersion> versions)
    {
        var list = versions.ToList();
        return list.FirstOrDefault(v => v.VersionType.Equals("release", StringComparison.OrdinalIgnoreCase))
            ?? list.FirstOrDefault(v => v.VersionType.Equals("beta", StringComparison.OrdinalIgnoreCase));
    }
}
