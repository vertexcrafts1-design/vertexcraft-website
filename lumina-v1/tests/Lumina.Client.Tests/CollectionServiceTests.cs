using System.Net;
using System.Text;
using CmlLib.Core.Auth;
using Lumina.Client.Services;
using Xunit;

namespace Lumina.Client.Tests;

public sealed class CollectionServiceTests
{
    [Fact]
    public async Task Verification_Outage_Fails_Closed_Without_Throwing()
    {
        var client = new HttpClient(new DelegateHandler(_ => throw new HttpRequestException("offline")))
        {
            BaseAddress = new Uri("https://lumina.test/")
        };

        var service = new VerificationService(client);
        Assert.False(await service.IsVerifiedAsync("abc"));
    }

    [Fact]
    public async Task Collection_Outage_Returns_Empty_List()
    {
        var client = new HttpClient(new DelegateHandler(_ => throw new HttpRequestException("offline")))
        {
            BaseAddress = new Uri("https://lumina.test/")
        };

        var service = new LuminaCollectionService(client);
        Assert.Empty(await service.GetAsync());
    }

    [Fact]
    public async Task Add_Uses_Minecraft_Access_Token_Not_Client_Supplied_Uuid()
    {
        HttpRequestMessage? captured = null;
        string? body = null;
        var client = new HttpClient(new DelegateHandler(async request =>
        {
            captured = request;
            body = request.Content is null ? null : await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("{\"item\":{\"projectId\":\"known-project\",\"category\":\"mod\",\"curatorUuid\":\"server-profile-id\",\"addedAt\":\"2026-09-15T18:00:00Z\"}}", Encoding.UTF8, "application/json")
            };
        }))
        {
            BaseAddress = new Uri("https://lumina.test/")
        };

        var service = new LuminaCollectionService(client);
        var session = new MSession("Player", "real-minecraft-token", "local-session-uuid");
        await service.AddAsync("known-project", "mod", "Fast", session);

        Assert.NotNull(captured);
        Assert.Equal("Bearer", captured!.Headers.Authorization?.Scheme);
        Assert.Equal("real-minecraft-token", captured.Headers.Authorization?.Parameter);
        Assert.Contains("known-project", body);
        Assert.DoesNotContain("local-session-uuid", body);
    }

    private sealed class DelegateHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => callback(request);
    }
}
