using GeneaPam.Api.UnitTests.Infrastructure.Adapters;

namespace GeneaPam.Api.UnitTests.Infrastructure.Jobs;

public sealed class InMemoryJobDispatcherTests
{
    private sealed record TestJob(string Name);

    private sealed record OtherJob(int Value);

    [Fact]
    public async Task SendAsync_CollectsDispatchedMessage()
    {
        var dispatcher = new InMemoryJobDispatcher();

        await dispatcher.SendAsync(new TestJob("hello"));

        var dispatched = dispatcher.Get<TestJob>();
        Assert.Single(dispatched);
        Assert.Equal("hello", dispatched[0].Name);
    }

    [Fact]
    public async Task Get_ReturnsOnlyMatchingType()
    {
        var dispatcher = new InMemoryJobDispatcher();

        await dispatcher.SendAsync(new TestJob("hello"));
        await dispatcher.SendAsync(new OtherJob(42));

        Assert.Single(dispatcher.Get<TestJob>());
        Assert.Single(dispatcher.Get<OtherJob>());
    }

    [Fact]
    public async Task Get_PreservesDispatchOrder()
    {
        var dispatcher = new InMemoryJobDispatcher();

        await dispatcher.SendAsync(new TestJob("first"));
        await dispatcher.SendAsync(new TestJob("second"));
        await dispatcher.SendAsync(new TestJob("third"));

        var dispatched = dispatcher.Get<TestJob>();
        Assert.Equal(["first", "second", "third"], dispatched.Select(j => j.Name));
    }
}
