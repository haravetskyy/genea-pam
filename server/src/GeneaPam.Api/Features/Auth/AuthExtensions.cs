using DnsClient;
using FluentValidation;
using Soenneker.Validators.Email.Disposable.Registrars;

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

        var authOptions =
            configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()
            ?? new AuthOptions();
        services.AddHttpClient(
            "hibp",
            client =>
            {
                client.BaseAddress = new Uri(authOptions.HibpBaseUrl.TrimEnd('/') + "/");
                client.DefaultRequestHeaders.Add("Add-Padding", "true");
            }
        );

        services.AddEmailDisposableValidatorAsSingleton();
        services.AddValidatorsFromAssemblyContaining<RegisterValidator>();

        return services;
    }
}
