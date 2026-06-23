namespace GeneaPam.Api.Infrastructure.Jobs;

public interface IJobDispatcher
{
    ValueTask SendAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class;
}
