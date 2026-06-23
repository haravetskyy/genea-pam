using GeneaPam.Api.Infrastructure.Storage;
using GeneaPam.Api.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GeneaPam.Api.IntegrationTests.Smoke;

public sealed class StorageConfigTests(ApiFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public void StorageOptions_SectionName_IsStorage()
    {
        Assert.Equal("Storage", StorageOptions.SectionName);
    }

    [Fact]
    public void StorageOptions_BoundFromConfig_HasNonEmptyEndpoint()
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<StorageOptions>>().Value;

        Assert.NotEmpty(options.Endpoint);
    }
}
