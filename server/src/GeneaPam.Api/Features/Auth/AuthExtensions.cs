using DnsClient;
using FluentValidation;

namespace GeneaPam.Api.Features.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddAuth(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddSingleton<ILookupClient>(_ => new LookupClient());
        services.AddSingleton<IDnsResolver, DnsClientResolver>();

        var hibpBaseUrl = configuration["HibpBaseUrl"] ?? "https://api.pwnedpasswords.com/";
        services.AddHttpClient(
            "hibp",
            client =>
            {
                client.BaseAddress = new Uri(hibpBaseUrl.TrimEnd('/') + "/");
                client.DefaultRequestHeaders.Add("Add-Padding", "true");
            }
        );

        services.AddValidatorsFromAssemblyContaining<RegisterValidator>();

        return services;
    }
}
