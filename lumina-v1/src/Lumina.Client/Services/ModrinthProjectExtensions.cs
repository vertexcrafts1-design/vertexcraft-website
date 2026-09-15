using System.Text.Json;
using System.Text.Json.Serialization;
using Lumina.Core;

namespace Lumina.Client.Services;

public static class ModrinthProjectExtensions
{
    private static readonly HttpClient Http = CreateClient();

    public static async Task<GalleryProject?> GetProjectAsync(
        this ModrinthService _,
        string projectId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(projectId)) return null;
        using var response = await Http.GetAsync(
            $"https://api.modrinth.com/v2/project/{Uri.EscapeDataString(projectId)}",
            cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        return new GalleryProject
        {
            ProjectId = String(root, "id") ?? projectId,
            Slug = String(root, "slug") ?? "",
            Title = String(root, "title") ?? projectId,
            Description = String(root, "description") ?? "",
            Author = "Modrinth",
            Downloads = root.TryGetProperty("downloads", out var downloads) && downloads.TryGetInt64(out var count) ? count : 0,
            IconUrl = String(root, "icon_url"),
            ProjectType = String(root, "project_type") ?? "mod",
            Categories = root.TryGetProperty("categories", out var categories) && categories.ValueKind == JsonValueKind.Array
                ? categories.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToList()
                : []
        };
    }

    public static async Task<IReadOnlyList<GalleryProject>> GetProjectsAsync(
        this ModrinthService service,
        IEnumerable<string> projectIds,
        CancellationToken cancellationToken = default)
    {
        var ids = projectIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var tasks = ids.Select(id => service.GetProjectAsync(id, cancellationToken));
        var values = await Task.WhenAll(tasks);
        return values.Where(x => x is not null).Select(x => x!).ToList();
    }

    public static async Task<IReadOnlyList<GalleryProject>> SearchModpacksAsync(
        this ModrinthService _,
        string query,
        InstanceProfile instance,
        string sort = "downloads",
        int limit = 40,
        CancellationToken cancellationToken = default)
    {
        var facets = JsonSerializer.Serialize(new[]
        {
            new[] { "project_type:modpack" },
            new[] { $"versions:{instance.Version}" }
        });
        var url = "https://api.modrinth.com/v2/search" +
                  $"?query={Uri.EscapeDataString(query ?? string.Empty)}" +
                  $"&facets={Uri.EscapeDataString(facets)}" +
                  $"&index={Uri.EscapeDataString(sort)}&limit={Math.Clamp(limit, 1, 100)}";
        using var response = await Http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return (await JsonSerializer.DeserializeAsync<ModrinthSearchResponse>(stream, cancellationToken: cancellationToken))?.Hits ?? [];
    }

    private static string? String(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("LUMINA/1.2 (Minecraft Client)");
        return client;
    }
}
