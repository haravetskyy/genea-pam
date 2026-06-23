using GeneaPam.Api.Infrastructure.Storage;
using GeneaPam.Api.IntegrationTests.Infrastructure;

namespace GeneaPam.Api.IntegrationTests.Smoke;

public sealed class StorageConfigTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public void StorageOptions_SectionName_IsStorage()
    {
        Assert.Equal("Storage", StorageOptions.SectionName);
    }
}
