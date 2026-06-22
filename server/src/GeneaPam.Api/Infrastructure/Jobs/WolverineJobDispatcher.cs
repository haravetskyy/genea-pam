using Wolverine;

namespace GeneaPam.Api.Infrastructure.Jobs;

public class WolverineJobDispatcher(IMessageBus bus) : IJobDispatcher
{
    public ValueTask SendAsync<T>(T message, CancellationToken cancellationToken = default) where T : class =>
        bus.SendAsync(message);
}
