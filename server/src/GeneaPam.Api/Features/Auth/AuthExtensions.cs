using System.Text;
using DnsClient;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Soenneker.Validators.Email.Disposable.Registrars;

namespace GeneaPam.Api.Features.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    ) =>
        services
            .AddAuthValidation(configuration)
            .AddAuthTokens(configuration, isProduction: !environment.IsDevelopment());

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
        IConfiguration configuration,
        bool isProduction = false
    )
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        services.AddScoped<ITokenIssuer, JwtTokenIssuer>();
        services.AddScoped<IRefreshTokenStore, DbRefreshTokenStore>();
        services.AddScoped<RefreshTokenCleanupJob>();
        services.AddHostedService<RefreshTokenCleanupService>();

        var authOptions =
            configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()
            ?? new AuthOptions();

        if (
            isProduction
            && (string.IsNullOrEmpty(authOptions.JwtSecret) || authOptions.JwtSecret.Length < 32)
        )
            throw new InvalidOperationException(
                "Auth:JwtSecret must be at least 32 characters in non-Development environments."
            );

        var jwtKey = string.IsNullOrEmpty(authOptions.JwtSecret)
            ? new string('x', 32)
            : authOptions.JwtSecret;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero,
                };
            });
        services.AddAuthorization();

        return services;
    }
}
