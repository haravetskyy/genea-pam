using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using GeneaPam.Api.IntegrationTests.Infrastructure;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace GeneaPam.Api.IntegrationTests.Features.Auth;

public sealed class LogoutTests(ApiFactory factory) : IntegrationTest(factory)
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
    public async Task Logout_ClearsRefreshCookie_AndSubsequentRefreshWith_OldToken_Returns401()
    {
        var refreshToken = await LoginAndGetRefreshTokenAsync("logout_test@gmail.com");

        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        logoutRequest.Headers.Add("Cookie", $"refresh_token={refreshToken}");
        var logoutResponse = await Client.SendAsync(logoutRequest);

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var setCookie = logoutResponse.Headers.GetValues("Set-Cookie").FirstOrDefault();
        Assert.NotNull(setCookie);
        Assert.Contains("refresh_token=;", setCookie);

        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"refresh_token={refreshToken}");
        var refreshResponse = await Client.SendAsync(refreshRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);

        var problem = await refreshResponse.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal("Auth.TokenInvalid", problem?.ErrorCode);
    }

    private sealed record ProblemResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("errorCode")]
        public string? ErrorCode { get; init; }
    }
}
