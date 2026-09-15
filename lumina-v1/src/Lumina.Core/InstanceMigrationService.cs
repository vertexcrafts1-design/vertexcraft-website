namespace Lumina.Core;

public sealed record InstanceMigrationResult(bool Changed, string Message);

public sealed class InstanceMigrationService
{
    public async Task<InstanceMigrationResult> MigrateAsync(
        InstanceProfile profile,
        string latestRelease,
        string latestSnapshot,
        IReadOnlyCollection<string> knownVersions,
        Func<string, string, Task<IReadOnlyList<string>>> getLoaderVersions)
    {
        var changed = false;

        if (profile.Version.Equals("latest-release", StringComparison.OrdinalIgnoreCase))
        {
            profile.Version = latestRelease;
            changed = true;
        }
        else if (profile.Version.Equals("latest-snapshot", StringComparison.OrdinalIgnoreCase) && profile.AllowSnapshots)
        {
            profile.Version = latestSnapshot;
            changed = true;
        }

        var normalizedLoader = NormalizeLoader(profile.Loader);
        if (!string.Equals(profile.Loader, normalizedLoader, StringComparison.Ordinal))
        {
            profile.Loader = normalizedLoader;
            changed = true;
        }

        IReadOnlyList<string> loaderVersions;
        if (profile.Loader.Equals("Vanilla", StringComparison.OrdinalIgnoreCase))
        {
            loaderVersions = ["Standard"];
            if (!profile.LoaderVersion.Equals("Standard", StringComparison.OrdinalIgnoreCase))
            {
                profile.LoaderVersion = "Standard";
                changed = true;
            }
        }
        else if (profile.Version.StartsWith("latest-", StringComparison.OrdinalIgnoreCase) ||
                 !knownVersions.Contains(profile.Version, StringComparer.OrdinalIgnoreCase))
        {
            loaderVersions = [];
        }
        else
        {
            loaderVersions = await getLoaderVersions(profile.Version, profile.Loader);
        }

        var validation = InstanceValidationService.Validate(profile, knownVersions, loaderVersions);
        profile.IsValid = validation.IsValid;
        profile.InvalidReason = validation.Error ?? "";
        return new InstanceMigrationResult(changed, profile.InvalidReason);
    }

    public static string NormalizeLoader(string loader) => loader.Trim().ToLowerInvariant() switch
    {
        "vanilla" => "Vanilla",
        "fabric" => "Fabric",
        "quilt" => "Quilt",
        "forge" => "Forge",
        "neoforge" or "neo forge" => "NeoForge",
        _ => loader.Trim()
    };
}
