using GeneaPam.Api.Features.Auth.Internal;

namespace GeneaPam.Api.IntegrationTests.Infrastructure.Adapters;

public sealed class FakeDnsResolver : IDnsResolver
{
    private readonly HashSet<string> _domainsWithMx = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _domainsWithoutMx = new(StringComparer.OrdinalIgnoreCase);

    public void AllowMx(string domain) => _domainsWithMx.Add(domain);

    public void DenyMx(string domain) => _domainsWithoutMx.Add(domain);

    public Task<bool> HasMxRecordAsync(string domain, CancellationToken cancellationToken = default)
    {
        if (_domainsWithoutMx.Contains(domain))
            return Task.FromResult(false);

        // default: allow (real domains like gmail.com used in tests)
        return Task.FromResult(!_domainsWithoutMx.Contains(domain));
    }
}
