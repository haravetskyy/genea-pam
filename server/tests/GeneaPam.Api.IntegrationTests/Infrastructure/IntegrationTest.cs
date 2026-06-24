using Microsoft.AspNetCore.Mvc.Testing;
using WireMock.Server;

namespace GeneaPam.Api.IntegrationTests.Infrastructure;

public abstract class IntegrationTest(ApiFactory factory) : IClassFixture<ApiFactory>
{
    protected HttpClient Client { get; } =
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    protected WireMockServer WireMock { get; } = factory.WireMock;
}
