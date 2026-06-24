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
        IConfiguration configuration
    )
    {
        services.AddSingleton<ILookupClient>(_ => new LookupClient());
        services.AddSingleton<IDnsResolver, DnsClientResolver>();

        var authOptions =
            configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()
            ?? new AuthOptions();

        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

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

        services.AddScoped<ITokenIssuer, JwtTokenIssuer>();
        services.AddScoped<IRefreshTokenStore, DbRefreshTokenStore>();

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
