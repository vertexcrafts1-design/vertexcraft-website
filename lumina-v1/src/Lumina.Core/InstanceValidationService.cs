namespace Lumina.Core;

public sealed record InstanceValidationResult(bool IsValid, string? Error)
{
    public static InstanceValidationResult Ok() => new(true, null);
    public static InstanceValidationResult Fail(string message) => new(false, message);
}

public static class InstanceValidationService
{
    private static readonly HashSet<string> SupportedLoaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Vanilla", "Fabric", "Quilt", "Forge", "NeoForge"
    };

    public static InstanceValidationResult Validate(
        InstanceProfile profile,
        IReadOnlyCollection<string> minecraftVersions,
        IReadOnlyCollection<string> loaderVersions)
    {
        if (string.IsNullOrWhiteSpace(profile.Version) ||
            profile.Version.StartsWith("latest-", StringComparison.OrdinalIgnoreCase))
            return InstanceValidationResult.Fail("Wähle eine konkrete Minecraft-Version.");

        if (!minecraftVersions.Contains(profile.Version, StringComparer.OrdinalIgnoreCase))
            return InstanceValidationResult.Fail($"Minecraft-Version {profile.Version} ist nicht im aktuellen Katalog verfügbar.");

        if (!SupportedLoaders.Contains(profile.Loader))
            return InstanceValidationResult.Fail($"Loader {profile.Loader} wird von LUMINA nicht unterstützt.");

        if (profile.Loader.Equals("Vanilla", StringComparison.OrdinalIgnoreCase))
        {
            if (!profile.LoaderVersion.Equals("Standard", StringComparison.OrdinalIgnoreCase))
                return InstanceValidationResult.Fail("Vanilla muss die Loader-Version Standard verwenden.");
            return InstanceValidationResult.Ok();
        }

        if (string.IsNullOrWhiteSpace(profile.LoaderVersion))
            return InstanceValidationResult.Fail($"Für {profile.Loader} fehlt eine Loader-Version.");

        if (!loaderVersions.Contains(profile.LoaderVersion, StringComparer.OrdinalIgnoreCase))
            return InstanceValidationResult.Fail($"{profile.Loader} {profile.LoaderVersion} ist für Minecraft {profile.Version} nicht verfügbar.");

        return InstanceValidationResult.Ok();
    }
}
