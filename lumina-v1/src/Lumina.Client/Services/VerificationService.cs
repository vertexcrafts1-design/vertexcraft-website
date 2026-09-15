using System.Net.Http.Json;
using System.Text.Json;

namespace Lumina.Client.Services;

public sealed class VerificationService
{
    public static readonly Uri DefaultBaseAddress = new("https://lumina-collection-api.vertexcrafts1.workers.dev/");
    private readonly HttpClient _http;

    public VerificationService(HttpClient? httpClient = null)
    {
        _http = httpClient ?? new HttpClient { BaseAddress = DefaultBaseAddress };
        _http.Timeout = TimeSpan.FromSeconds(8);
        if (_http.BaseAddress is null) _http.BaseAddress = DefaultBaseAddress;
    }

    public async Task<bool> IsVerifiedAsync(string minecraftUuid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(minecraftUuid)) return false;

        try
        {
            using var response = await _http.GetAsync(
                $"v1/verification/{Uri.EscapeDataString(minecraftUuid.Trim())}",
                cancellationToken);
            if (!response.IsSuccessStatusCode) return false;

            var value = await response.Content.ReadFromJsonAsync<VerificationResponse>(cancellationToken: cancellationToken);
            return value?.Verified == true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return false;
        }
    }

    private sealed class VerificationResponse
    {
        public bool Verified { get; set; }
    }
}
