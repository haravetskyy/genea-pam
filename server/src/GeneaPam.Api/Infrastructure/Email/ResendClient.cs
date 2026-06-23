using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace GeneaPam.Api.Infrastructure.Email;

public sealed class ResendClient(HttpClient http, IOptions<ResendOptions> options)
{
    private readonly ResendOptions _options = options.Value;

    public async Task SendAsync(
        string to,
        string subject,
        string html,
        CancellationToken cancellationToken = default
    )
    {
        var payload = new
        {
            from = _options.FromAddress,
            to,
            subject,
            html,
        };
        var response = await http.PostAsJsonAsync("/emails", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
