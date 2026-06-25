using System.Text;
using GeneaPam.Api.Features.Auth.Internal;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace GeneaPam.Api.Infrastructure.Auth;

public static class JwtBearerExtensions
{
    public static IServiceCollection AddJwtBearer(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        var authOptions =
            configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()
            ?? new AuthOptions();

        if (
            !environment.IsDevelopment()
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
