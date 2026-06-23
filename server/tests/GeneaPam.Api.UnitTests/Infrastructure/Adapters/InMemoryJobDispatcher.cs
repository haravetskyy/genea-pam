using GeneaPam.Api.Infrastructure.Jobs;

namespace GeneaPam.Api.UnitTests.Infrastructure.Adapters;

public sealed class InMemoryJobDispatcher : IJobDispatcher
{
    private readonly List<object> _dispatched = [];

    public IReadOnlyList<T> Get<T>()
        where T : class => _dispatched.OfType<T>().ToList();

    public ValueTask SendAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        _dispatched.Add(message);
        return ValueTask.CompletedTask;
    }
}
