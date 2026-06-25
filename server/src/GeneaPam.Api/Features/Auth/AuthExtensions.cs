using DnsClient;
using FluentValidation;
using GeneaPam.Api.Features.Auth.Internal;
using GeneaPam.Api.Features.Auth.Refresh;
using GeneaPam.Api.Features.Auth.Register;
using Microsoft.Extensions.Options;
using Soenneker.Validators.Email.Disposable.Registrars;

namespace GeneaPam.Api.Features.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddAuth(
        this IServiceCollection services,
        IConfiguration configuration
    ) => services.AddAuthValidation(configuration).AddAuthTokens(configuration);

    public static IServiceCollection AddAuthValidation(
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

    public static IServiceCollection AddAuthTokens(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        services.AddScoped<ITokenIssuer, JwtTokenIssuer>();
        services.AddScoped<IRefreshTokenStore, DbRefreshTokenStore>();
        services.AddScoped<RefreshTokenCleanupJob>();
        services.AddHostedService<RefreshTokenCleanupService>();

        return services;
    }
}
