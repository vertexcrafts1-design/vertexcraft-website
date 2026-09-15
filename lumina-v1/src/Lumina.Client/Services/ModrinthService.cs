using System.Net.Http.Json;
using System.Text.Json;
using Lumina.Core;

namespace Lumina.Client.Services;

public sealed class ModrinthService
{
    private readonly HttpClient _http;
    private const string Api = "https://api.modrinth.com/v2";

    public ModrinthService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("LUMINA/1.1 (Minecraft Client)");
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
            [$"project_type:{projectType}"],
            [$"versions:{instance.Version}"]
        };

        if (projectType == "mod")
        {
            var loader = LoaderForModrinth(instance.Loader);
            if (loader == "minecraft") return [];
            facets.Add([$"categories:{loader}"]);
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
        var target = kind switch
        {
            "Resource Packs" => paths.ResourcePacks(instance.Id),
            "Shaders" => paths.ShaderPacks(instance.Id),
            _ => paths.Mods(instance.Id)
        };
        Directory.CreateDirectory(target);

        var installed = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await InstallProjectInternal(project.ProjectId, kind, instance, target, installed, visited, progress, cancellationToken);
        return new GalleryInstallResult(installed.Count, installed);
    }

    private async Task InstallProjectInternal(
        string projectId,
        string kind,
        InstanceProfile instance,
        string target,
        List<string> installed,
        HashSet<string> visited,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        if (!visited.Add(projectId)) return;

        var versions = await GetCompatibleVersions(projectId, kind, instance, cancellationToken);
        var version = versions.FirstOrDefault(v => string.Equals(v.VersionType, "release", StringComparison.OrdinalIgnoreCase))
                      ?? versions.FirstOrDefault()
                      ?? throw new InvalidOperationException("Für diese Minecraft-Version und diesen Loader gibt es keine kompatible Version.");

        var file = version.Files.FirstOrDefault(f => f.Primary) ?? version.Files.FirstOrDefault()
                   ?? throw new InvalidOperationException("Modrinth hat für diese Version keine herunterladbare Datei geliefert.");

        progress?.Report($"Lade {file.Filename} …");
        var output = Path.Combine(target, SanitizeFileName(file.Filename));
        await DownloadFile(file.Url, output, cancellationToken);
        installed.Add(Path.GetFileName(output));

        if (!string.Equals(kind, "Mods", StringComparison.OrdinalIgnoreCase)) return;

        foreach (var dep in version.Dependencies.Where(d => d.DependencyType == "required" && !string.IsNullOrWhiteSpace(d.ProjectId)))
        {
            progress?.Report("Installiere benötigte Abhängigkeit …");
            await InstallProjectInternal(dep.ProjectId!, kind, instance, target, installed, visited, progress, cancellationToken);
        }
    }

    private async Task<IReadOnlyList<ModrinthVersion>> GetCompatibleVersions(
        string projectId,
        string kind,
        InstanceProfile instance,
        CancellationToken cancellationToken)
    {
        var gameVersions = JsonSerializer.Serialize(new[] { instance.Version });
        var url = $"{Api}/project/{Uri.EscapeDataString(projectId)}/version?game_versions={Uri.EscapeDataString(gameVersions)}&include_changelog=false";

        if (string.Equals(kind, "Mods", StringComparison.OrdinalIgnoreCase))
        {
            var loader = LoaderForModrinth(instance.Loader);
            var loaders = JsonSerializer.Serialize(new[] { loader });
            url += $"&loaders={Uri.EscapeDataString(loaders)}";
        }

        using var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ModrinthVersion>>(cancellationToken: cancellationToken) ?? [];
    }

    private async Task DownloadFile(string url, string output, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var file = File.Create(output);
        await input.CopyToAsync(file, cancellationToken);
    }

    public static string LoaderForModrinth(string loader) => loader.Trim().ToLowerInvariant() switch
    {
        "fabric" => "fabric",
        "quilt" => "quilt",
        "forge" => "forge",
        "neoforge" => "neoforge",
        _ => "minecraft"
    };

    private static string KindToProjectType(string kind) => kind switch
    {
        "Resource Packs" => "resourcepack",
        "Shaders" => "shader",
        _ => "mod"
    };

    private static string SanitizeFileName(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
        return value;
    }
}
