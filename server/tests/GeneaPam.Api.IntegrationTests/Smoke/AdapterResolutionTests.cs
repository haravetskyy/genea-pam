using GeneaPam.Api.Infrastructure.Messaging;
using GeneaPam.Api.Infrastructure.Observability;
using GeneaPam.Api.Infrastructure.Storage;
using GeneaPam.Api.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GeneaPam.Api.IntegrationTests.Smoke;

public sealed class AdapterResolutionTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public void IObservabilityAdapter_ResolvesFromDi()
    {
        using var scope = factory.Services.CreateScope();
        var adapter = scope.ServiceProvider.GetRequiredService<IObservabilityAdapter>();
        Assert.NotNull(adapter);
    }

    [Fact]
    public void IObjectStorage_ResolvesFromDi()
    {
        using var scope = factory.Services.CreateScope();
        var adapter = scope.ServiceProvider.GetRequiredService<IObjectStorage>();
        Assert.NotNull(adapter);
    }

    [Fact]
    public void IMessageBroker_ResolvesFromDi()
    {
        using var scope = factory.Services.CreateScope();
        var adapter = scope.ServiceProvider.GetRequiredService<IMessageBroker>();
        Assert.NotNull(adapter);
    }
}
