using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using GeneaPam.Api.Features.Auth.Login;
using GeneaPam.Api.IntegrationTests.Infrastructure;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace GeneaPam.Api.IntegrationTests.Features.Auth.Refresh;

public sealed class RefreshTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string SafePassword = "SafeP@ss!99xyz";

    private static string HibpPrefix(string password)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(password));
        return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant()[..5];
    }

    private void StubHibpClean(string password)
    {
        var prefix = HibpPrefix(password);
        WireMock
            .Given(Request.Create().WithPath($"/range/{prefix}").UsingGet())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithBody("0000000000000000000000000000000000001:0\r\n")
            );
    }

    private async Task<string> LoginAndGetRefreshTokenAsync(string email)
    {
        StubHibpClean(SafePassword);
        await Client.PostAsJsonAsync(
            "/auth/register",
            new
            {
                email,
                password = SafePassword,
                displayName = "Test User",
            }
        );
        var loginResp = await Client.PostAsJsonAsync(
            "/auth/login",
            new { email, password = SafePassword }
        );
        var setCookie = loginResp.Headers.GetValues("Set-Cookie").First();
        var tokenPart = setCookie.Split(';')[0];
        return tokenPart.Substring("refresh_token=".Length);
    }

    [Fact]
    public async Task Refresh_WithValidCookie_Returns200WithNewAccessTokenAndRotatedCookie()
    {
        var refreshToken = await LoginAndGetRefreshTokenAsync("refresh_valid@gmail.com");

        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        request.Headers.Add("Cookie", $"refresh_token={refreshToken}");
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));

        var setCookie = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
        Assert.NotNull(setCookie);
        Assert.Contains("refresh_token=", setCookie);
        Assert.DoesNotContain(refreshToken, setCookie);
    }

    [Fact]
    public async Task Refresh_WithReplayedToken_Returns401()
    {
        var refreshToken = await LoginAndGetRefreshTokenAsync("refresh_replay@gmail.com");

        var first = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        first.Headers.Add("Cookie", $"refresh_token={refreshToken}");
        await Client.SendAsync(first);

        var second = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        second.Headers.Add("Cookie", $"refresh_token={refreshToken}");
        var response = await Client.SendAsync(second);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal("Auth.TokenInvalid", problem?.ErrorCode);
    }

    [Fact]
    public async Task Refresh_ConcurrentReplay_OnlyOneRequestSucceeds()
    {
        var refreshToken = await LoginAndGetRefreshTokenAsync("refresh_concurrent@gmail.com");

        var task1 = Task.Run(async () =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
            req.Headers.Add("Cookie", $"refresh_token={refreshToken}");
            return await Client.SendAsync(req);
        });

        var task2 = Task.Run(async () =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
            req.Headers.Add("Cookie", $"refresh_token={refreshToken}");
            return await Client.SendAsync(req);
        });

        var responses = await Task.WhenAll(task1, task2);
        var statusCodes = responses.Select(r => (int)r.StatusCode).ToArray();

        Assert.Contains(200, statusCodes);
        Assert.Contains(401, statusCodes);
    }

    [Fact]
    public async Task Refresh_WithMissingCookie_Returns401()
    {
        var response = await Client.PostAsync("/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal("Auth.TokenInvalid", problem?.ErrorCode);
    }

    private sealed record ProblemResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("errorCode")]
        public string? ErrorCode { get; init; }
    }
}
