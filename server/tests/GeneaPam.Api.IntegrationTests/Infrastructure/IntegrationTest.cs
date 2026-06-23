using WireMock.Server;

namespace GeneaPam.Api.IntegrationTests.Infrastructure;

public abstract class IntegrationTest(ApiFactory factory) : IClassFixture<ApiFactory>
{
    protected HttpClient Client { get; } = factory.CreateClient();
    protected WireMockServer WireMock { get; } = factory.WireMock;
}
