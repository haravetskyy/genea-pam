using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using GeneaPam.Api.Features.Auth;
using GeneaPam.Api.IntegrationTests.Infrastructure;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace GeneaPam.Api.IntegrationTests.Auth;

public sealed class LoginTests(ApiFactory factory) : IntegrationTest(factory)
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

    private async Task RegisterUserAsync(string email)
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
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithAccessTokenAndRefreshCookie()
    {
        await RegisterUserAsync("login_valid@gmail.com");

        var response = await Client.PostAsJsonAsync(
            "/auth/login",
            new { email = "login_valid@gmail.com", password = SafePassword }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));

        var setCookie = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
        Assert.NotNull(setCookie);
        Assert.Contains("refresh_token=", setCookie);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401WithInvalidCredentials()
    {
        await RegisterUserAsync("login_wrongpwd@gmail.com");

        var response = await Client.PostAsJsonAsync(
            "/auth/login",
            new { email = "login_wrongpwd@gmail.com", password = "WrongPass!99" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal("Auth.InvalidCredentials", problem?.ErrorCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401WithInvalidCredentials()
    {
        var response = await Client.PostAsJsonAsync(
            "/auth/login",
            new { email = "nobody@gmail.com", password = SafePassword }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal("Auth.InvalidCredentials", problem?.ErrorCode);
    }

    private sealed record ProblemResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("errorCode")]
        public string? ErrorCode { get; init; }
    }
}
