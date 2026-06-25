namespace GeneaPam.Api.Features.Auth.Internal;

public interface IDnsResolver
{
    Task<bool> HasMxRecordAsync(string domain, CancellationToken cancellationToken = default);
}
