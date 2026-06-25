using System.Text;
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
        var jwtOptions =
            configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (
            !environment.IsDevelopment()
            && (string.IsNullOrEmpty(jwtOptions.JwtSecret) || jwtOptions.JwtSecret.Length < 32)
        )
            throw new InvalidOperationException(
                "Auth:JwtSecret must be at least 32 characters in non-Development environments."
            );

        var jwtKey = string.IsNullOrEmpty(jwtOptions.JwtSecret)
            ? new string('x', 32)
            : jwtOptions.JwtSecret;

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

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy =
                new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
                    JwtBearerDefaults.AuthenticationScheme
                )
                    .RequireAuthenticatedUser()
                    .Build();
        });

        return services;
    }
}
