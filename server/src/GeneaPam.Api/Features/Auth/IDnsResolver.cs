namespace GeneaPam.Api.Features.Auth;

public interface IDnsResolver
{
    Task<bool> HasMxRecordAsync(string domain, CancellationToken cancellationToken = default);
}
