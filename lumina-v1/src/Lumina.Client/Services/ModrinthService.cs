using System.Net.Http.Json;
using System.Text.Json;
using Lumina.Core;

namespace Lumina.Client.Services;

public sealed class ModrinthService
{
    private readonly HttpClient _http;
    private const string Api = "https://api.modrinth.com/v2";

    public ModrinthService(HttpClient? httpClient = null)
    {
        _http = httpClient ?? new HttpClient();
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("LUMINA/1.2 (Minecraft Client)");
        _http.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<IReadOnlyList<GalleryProject>> SearchAsync(
        string query,
        string kind,
        InstanceProfile instance,
        string sort = "downloads",
        int limit = 30,
        CancellationToken cancellationToken = default)
    {
        var projectType = KindToProjectType(kind);
        var facets = new List<string[]>
        {
            new[] { $"project_type:{projectType}" },
            new[] { $"versions:{instance.Version}" }
        };

        if (projectType == "mod")
        {
            var loader = LoaderForModrinth(instance.Loader);
            if (loader == "minecraft") return [];
            facets.Add(new[] { $"categories:{loader}" });
        }

        var facetsJson = JsonSerializer.Serialize(facets);
        var url = $"{Api}/search?query={Uri.EscapeDataString(query ?? string.Empty)}" +
                  $"&facets={Uri.EscapeDataString(facetsJson)}" +
                  $"&index={Uri.EscapeDataString(sort)}&limit={Math.Clamp(limit, 1, 100)}";

        using var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<ModrinthSearchResponse>(cancellationToken: cancellationToken);
        return data?.Hits ?? [];
    }

    public async Task<GalleryInstallResult> InstallAsync(
        GalleryProject project,
        string kind,
        InstanceProfile instance,
        AppPaths paths,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var plan = await BuildInstallPlanAsync(project.ProjectId, kind, instance, cancellationToken);
        return await ApplyInstallPlanAsync(plan, instance, paths, progress, cancellationToken);
    }

    public async Task<ModrinthInstallPlan> BuildInstallPlanAsync(
        string projectId,
        string kind,
        InstanceProfile instance,
        CancellationToken cancellationToken = default)
    {
        var contentKind = KindToContentKind(kind);
        var files = new List<ModrinthPlannedFile>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await ResolveProjectAsync(projectId, contentKind, instance, files, visited, cancellationToken);
        return new ModrinthInstallPlan(files);
    }

    private async Task ResolveProjectAsync(
        string projectId,
        ContentKind kind,
        InstanceProfile instance,
        List<ModrinthPlannedFile> files,
        HashSet<string> visited,
        CancellationToken cancellationToken)
    {
        if (!visited.Add(projectId)) return;

        var versions = await GetCompatibleVersionsAsync(projectId, kind, instance, cancellationToken);
        var compatible = versions.Where(v => CompatibilityService.Check(instance, kind, v.Descriptor).IsCompatible).ToList();
        var version = CompatibilityService.ChoosePreferred(compatible)
                      ?? throw new InvalidOperationException(
                          $"Für '{projectId}' gibt es keine kompatible Release- oder Beta-Version für Minecraft {instance.Version} / {instance.Loader}.");

        var file = version.Files.FirstOrDefault(f => f.Primary) ?? version.Files.FirstOrDefault()
                   ?? throw new InvalidOperationException("Modrinth hat für die ausgewählte Version keine Datei geliefert.");

        file.Hashes.TryGetValue("sha512", out var sha512);
        file.Hashes.TryGetValue("sha1", out var sha1);
        files.Add(new ModrinthPlannedFile(
            version.ProjectId,
            version.Id,
            SanitizeFileName(file.Filename),
            file.Url,
            sha512,
            sha1,
            kind));

        if (kind != ContentKind.Mod) return;

        foreach (var dependency in version.Dependencies.Where(d =>
                     d.DependencyType.Equals("required", StringComparison.OrdinalIgnoreCase)))
        {
            var dependencyProjectId = dependency.ProjectId;
            if (string.IsNullOrWhiteSpace(dependencyProjectId) && !string.IsNullOrWhiteSpace(dependency.VersionId))
            {
                var dependencyVersion = await GetVersionAsync(dependency.VersionId!, cancellationToken);
                dependencyProjectId = dependencyVersion?.ProjectId;
            }

            if (string.IsNullOrWhiteSpace(dependencyProjectId))
                throw new InvalidOperationException("Eine benötigte Modrinth-Abhängigkeit konnte nicht aufgelöst werden.");

            await ResolveProjectAsync(dependencyProjectId, kind, instance, files, visited, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<ModrinthVersion>> GetCompatibleVersionsAsync(
        string projectId,
        ContentKind kind,
        InstanceProfile instance,
        CancellationToken cancellationToken = default)
    {
        var gameVersions = JsonSerializer.Serialize(new[] { instance.Version });
        var url = $"{Api}/project/{Uri.EscapeDataString(projectId)}/version?game_versions={Uri.EscapeDataString(gameVersions)}&include_changelog=false";

        if (kind == ContentKind.Mod)
        {
            var loader = LoaderForModrinth(instance.Loader);
            if (loader == "minecraft") return [];
            var loaders = JsonSerializer.Serialize(new[] { loader });
            url += $"&loaders={Uri.EscapeDataString(loaders)}";
        }

        using var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ModrinthVersion>>(cancellationToken: cancellationToken) ?? [];
    }

    public async Task<ModrinthVersion?> GetVersionAsync(string versionId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"{Api}/version/{Uri.EscapeDataString(versionId)}", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ModrinthVersion>(cancellationToken: cancellationToken);
    }

    public async Task<GalleryInstallResult> ApplyInstallPlanAsync(
        ModrinthInstallPlan plan,
        InstanceProfile instance,
        AppPaths paths,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (plan.Files.Count == 0) return new GalleryInstallResult(0, []);

        var store = new ManagedContentStore(paths);
        var managed = store.Load(instance.Id).ToList();
        var staged = new List<(ModrinthPlannedFile Item, string Temp, string Final)>();

        try
        {
            foreach (var item in plan.Files)
            {
                var targetFolder = FolderFor(paths, instance.Id, item.Kind);
                Directory.CreateDirectory(targetFolder);
                var finalPath = Path.Combine(targetFolder, item.FileName);
                var existingOwner = managed.FirstOrDefault(x =>
                    x.Kind == item.Kind && x.FileName.Equals(item.FileName, StringComparison.OrdinalIgnoreCase));

                if (File.Exists(finalPath) &&
                    (existingOwner is null || !string.Equals(existingOwner.ProjectId, item.ProjectId, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException(
                        $"'{item.FileName}' existiert bereits als lokaler oder anderer Content. LUMINA überschreibt diese Datei nicht automatisch.");

                var tempPath = FileIntegrityService.TemporaryPath(finalPath);
                if (File.Exists(tempPath)) File.Delete(tempPath);
                progress?.Report($"Lade {item.FileName} …");
                await DownloadToAsync(item.Url, tempPath, cancellationToken);

                if (!await FileIntegrityService.VerifyAsync(tempPath, item.Sha512, item.Sha1, cancellationToken))
                    throw new InvalidDataException($"Hash-Prüfung für '{item.FileName}' fehlgeschlagen.");

                staged.Add((item, tempPath, finalPath));
            }

            foreach (var entry in staged)
            {
                File.Move(entry.Temp, entry.Final, true);
                var record = new ManagedContentRecord
                {
                    ProjectId = entry.Item.ProjectId,
                    VersionId = entry.Item.VersionId,
                    FileName = entry.Item.FileName,
                    Sha512 = entry.Item.Sha512,
                    Sha1 = entry.Item.Sha1,
                    Kind = entry.Item.Kind,
                    InstalledUtc = DateTime.UtcNow
                };
                store.Upsert(instance.Id, record);
            }

            return new GalleryInstallResult(staged.Count, staged.Select(x => x.Item.FileName).ToList());
        }
        catch
        {
            foreach (var entry in staged)
            {
                try { if (File.Exists(entry.Temp)) File.Delete(entry.Temp); }
                catch { }
            }
            throw;
        }
    }

    private async Task DownloadToAsync(string url, string path, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await input.CopyToAsync(output, cancellationToken);
    }

    public static string LoaderForModrinth(string loader) => loader.Trim().ToLowerInvariant() switch
    {
        "fabric" => "fabric",
        "quilt" => "quilt",
        "forge" => "forge",
        "neoforge" => "neoforge",
        _ => "minecraft"
    };

    public static ContentKind KindToContentKind(string kind) => kind switch
    {
        "Resource Packs" => ContentKind.ResourcePack,
        "Shaders" => ContentKind.ShaderPack,
        _ => ContentKind.Mod
    };

    private static string KindToProjectType(string kind) => kind switch
    {
        "Resource Packs" => "resourcepack",
        "Shaders" => "shader",
        _ => "mod"
    };

    private static string FolderFor(AppPaths paths, string instanceId, ContentKind kind) => kind switch
    {
        ContentKind.ResourcePack => paths.ResourcePacks(instanceId),
        ContentKind.ShaderPack => paths.ShaderPacks(instanceId),
        _ => paths.Mods(instanceId)
    };

    private static string SanitizeFileName(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
        return value;
    }
}
