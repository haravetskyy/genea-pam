using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using GeneaPam.Api.Features.Auth;
using GeneaPam.Api.Infrastructure.Jobs;
using GeneaPam.Api.IntegrationTests.Infrastructure;
using GeneaPam.Api.UnitTests.Infrastructure.Adapters;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace GeneaPam.Api.IntegrationTests.Features.Auth;

public sealed class RegistrationTests(ApiFactory factory) : IntegrationTest(factory)
{
    // "password123" SHA-1 = "CBFDAC6008F9CAB4083784CBD1874F76618D2A97"
    // prefix = "CBFDA", suffix = "C6008F9CAB4083784CBD1874F76618D2A97"
    private const string SafePassword = "SafeP@ss!99xyz";
    private const string BreachedPassword = "password123";

    private static string HibpPrefix(string password)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(password));
        return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant()[..5];
    }

    private static string HibpSuffix(string password)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(password));
        return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant()[5..];
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

    private void StubHibpBreached(string password)
    {
        var prefix = HibpPrefix(password);
        var suffix = HibpSuffix(password);
        WireMock
            .Given(Request.Create().WithPath($"/range/{prefix}").UsingGet())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithBody($"{suffix}:42\r\n0000000000000000000000000000000000001:0\r\n")
            );
    }

    [Fact]
    public async Task Register_WithValidInput_Returns201AndRegisterResponse()
    {
        StubHibpClean(SafePassword);
        factory.DnsResolver.AllowMx("gmail.com");

        var response = await Client.PostAsJsonAsync(
            "/auth/register",
            new
            {
                email = "alice@gmail.com",
                password = SafePassword,
                displayName = "Alice",
            }
        );

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty.ToString(), body.UserId);
        Assert.Equal("Alice", body.DisplayName);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        StubHibpClean(SafePassword);
        factory.DnsResolver.AllowMx("gmail.com");

        var payload = new
        {
            email = "bob@gmail.com",
            password = SafePassword,
            displayName = "Bob",
        };

        var first = await Client.PostAsJsonAsync("/auth/register", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        StubHibpClean(SafePassword);
        var second = await Client.PostAsJsonAsync("/auth/register", payload);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var problem = await second.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal("Auth.EmailAlreadyTaken", problem?.ErrorCode);
    }

    [Fact]
    public async Task Register_WithBreachedPassword_Returns422()
    {
        StubHibpBreached(BreachedPassword);
        factory.DnsResolver.AllowMx("gmail.com");

        var response = await Client.PostAsJsonAsync(
            "/auth/register",
            new
            {
                email = "carol@gmail.com",
                password = BreachedPassword,
                displayName = "Carol",
            }
        );

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal("Auth.PasswordBreached", problem?.ErrorCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmailSyntax_Returns422()
    {
        StubHibpClean(SafePassword);

        var response = await Client.PostAsJsonAsync(
            "/auth/register",
            new
            {
                email = "not-an-email",
                password = SafePassword,
                displayName = "Dave",
            }
        );

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal("Auth.EmailInvalid", problem?.ErrorCode);
    }

    [Fact]
    public async Task Register_WithDisposableEmail_Returns422()
    {
        StubHibpClean(SafePassword);

        var response = await Client.PostAsJsonAsync(
            "/auth/register",
            new
            {
                email = "eve@mailinator.com",
                password = SafePassword,
                displayName = "Eve",
            }
        );

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal("Auth.EmailDisposable", problem?.ErrorCode);
    }

    [Fact]
    public async Task Register_WithNoMxRecord_Returns422()
    {
        StubHibpClean(SafePassword);
        factory.DnsResolver.DenyMx("nomx.example");

        var response = await Client.PostAsJsonAsync(
            "/auth/register",
            new
            {
                email = "frank@nomx.example",
                password = SafePassword,
                displayName = "Frank",
            }
        );

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal("Auth.EmailInvalid", problem?.ErrorCode);
    }

    [Fact]
    public async Task Register_WithShortNonBreachedPassword_Returns422WithPasswordTooShort()
    {
        const string shortPassword = "Ab1!x";
        StubHibpClean(shortPassword);
        factory.DnsResolver.AllowMx("gmail.com");

        var response = await Client.PostAsJsonAsync(
            "/auth/register",
            new
            {
                email = "henry@gmail.com",
                password = shortPassword,
                displayName = "Henry",
            }
        );

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal("Auth.PasswordTooShort", problem?.ErrorCode);
    }

    [Fact]
    public async Task Register_WithValidInput_EnqueuesWelcomeEmailJob()
    {
        StubHibpClean(SafePassword);
        factory.DnsResolver.AllowMx("gmail.com");

        await Client.PostAsJsonAsync(
            "/auth/register",
            new
            {
                email = "grace@gmail.com",
                password = SafePassword,
                displayName = "Grace",
            }
        );

        var dispatcher = factory.Services.GetRequiredService<InMemoryJobDispatcher>();

        var jobs = dispatcher.Get<WelcomeEmailJob>();
        var job = jobs.FirstOrDefault(j => j.To == "grace@gmail.com");
        Assert.NotNull(job);
        Assert.Equal("Grace", job.UserName);
    }

    private sealed record ProblemResponse(string? ErrorCode)
    {
        public ProblemResponse()
            : this((string?)null) { }

        [System.Text.Json.Serialization.JsonPropertyName("errorCode")]
        public string? ErrorCode { get; init; } = ErrorCode;
    }
}
