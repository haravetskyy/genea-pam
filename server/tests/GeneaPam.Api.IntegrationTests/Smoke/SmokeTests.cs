using System.Net;
using System.Net.Http.Json;
using GeneaPam.Api.IntegrationTests.Infrastructure;

namespace GeneaPam.Api.IntegrationTests.Smoke;

public sealed class SmokeTests(ApiFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task HealthEndpoint_Returns200()
    {
        var response = await Client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnhandledException_Returns500ProblemDetails()
    {
        var response = await Client.GetAsync("/test/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(body);
        Assert.True(body.ContainsKey("title") || body.ContainsKey("status"));
    }
}
