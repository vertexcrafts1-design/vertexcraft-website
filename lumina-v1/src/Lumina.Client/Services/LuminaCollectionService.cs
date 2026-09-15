using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CmlLib.Core.Auth;

namespace Lumina.Client.Services;

public sealed class LuminaCollectionService
{
    private readonly HttpClient _http;

    public LuminaCollectionService(HttpClient? httpClient = null)
    {
        _http = httpClient ?? new HttpClient { BaseAddress = VerificationService.DefaultBaseAddress };
        _http.Timeout = TimeSpan.FromSeconds(10);
        if (_http.BaseAddress is null) _http.BaseAddress = VerificationService.DefaultBaseAddress;
    }

    public async Task<IReadOnlyList<LuminaCollectionEntry>> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _http.GetAsync("v1/collection", cancellationToken);
            if (!response.IsSuccessStatusCode) return [];

            var value = await response.Content.ReadFromJsonAsync<CollectionResponse>(cancellationToken: cancellationToken);
            return value?.Items ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return [];
        }
    }

    public async Task<LuminaCollectionEntry?> AddAsync(
        string projectId,
        string category,
        string? note,
        MSession session,
        CancellationToken cancellationToken = default)
    {
        if (session is null || !session.CheckIsValid() || string.IsNullOrWhiteSpace(session.AccessToken))
            throw new InvalidOperationException("Du musst mit einem gültigen Microsoft/Minecraft-Account angemeldet sein.");
        if (string.IsNullOrWhiteSpace(projectId))
            throw new ArgumentException("Modrinth-Projekt-ID fehlt.", nameof(projectId));

        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/collection")
        {
            Content = JsonContent.Create(new
            {
                projectId = projectId.Trim(),
                category = NormalizeCategory(category),
                note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        using var response = await _http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new UnauthorizedAccessException("Dieser Minecraft-Account ist nicht als LUMINA-Kurator verifiziert.");
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Deine Minecraft-Sitzung ist abgelaufen. Bitte melde dich erneut an.");
        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new InvalidOperationException("Dieses Modrinth-Projekt ist bereits in der LUMINA Collection.");
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException("Das Modrinth-Projekt wurde nicht gefunden.");

        response.EnsureSuccessStatusCode();
        var value = await response.Content.ReadFromJsonAsync<CollectionItemResponse>(cancellationToken: cancellationToken);
        return value?.Item;
    }

    private static string NormalizeCategory(string value) => value.Trim().ToLowerInvariant() switch
    {
        "mods" or "mod" => "mod",
        "resource packs" or "resourcepack" or "resourcepacks" => "resourcepack",
        "shaders" or "shader" => "shader",
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Unbekannte Collection-Kategorie.")
    };

    private sealed class CollectionResponse
    {
        public List<LuminaCollectionEntry> Items { get; set; } = [];
    }

    private sealed class CollectionItemResponse
    {
        public LuminaCollectionEntry? Item { get; set; }
    }
}

public sealed class LuminaCollectionEntry
{
    public string ProjectId { get; set; } = "";
    public string Category { get; set; } = "mod";
    public string CuratorUuid { get; set; } = "";
    public DateTimeOffset AddedAt { get; set; }
    public string? Note { get; set; }
}
