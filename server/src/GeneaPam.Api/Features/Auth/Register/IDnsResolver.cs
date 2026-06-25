namespace GeneaPam.Api.Features.Auth.Register;

public interface IDnsResolver
{
    Task<bool> HasMxRecordAsync(string domain, CancellationToken cancellationToken = default);
}
