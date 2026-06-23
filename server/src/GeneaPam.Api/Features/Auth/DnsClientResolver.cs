using DnsClient;

namespace GeneaPam.Api.Features.Auth;

public sealed class DnsClientResolver(ILookupClient lookupClient) : IDnsResolver
{
    public async Task<bool> HasMxRecordAsync(
        string domain,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var result = await lookupClient.QueryAsync(
                domain,
                QueryType.MX,
                cancellationToken: cancellationToken
            );
            return result.Answers.MxRecords().Any();
        }
        catch
        {
            return false;
        }
    }
}
