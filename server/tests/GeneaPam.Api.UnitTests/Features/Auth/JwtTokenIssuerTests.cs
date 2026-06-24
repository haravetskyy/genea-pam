using System.IdentityModel.Tokens.Jwt;
using GeneaPam.Api.Features.Auth;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace GeneaPam.Api.UnitTests.Features.Auth;

public sealed class JwtTokenIssuerTests
{
    private static JwtTokenIssuer BuildIssuer(int expiryMinutes = 15) =>
        new(
            Options.Create(
                new AuthOptions
                {
                    JwtSecret = "test-secret-key-that-is-at-least-32-chars!!",
                    JwtExpiryMinutes = expiryMinutes,
                }
            )
        );

    private static ApplicationUser TestUser() =>
        new()
        {
            Id = "user-123",
            Email = "alice@example.com",
            UserName = "alice@example.com",
        };

    [Fact]
    public void CreateAccessToken_ContainsSubClaim()
    {
        var issuer = BuildIssuer();
        var token = issuer.CreateAccessToken(TestUser());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("user-123", jwt.Subject);
    }

    [Fact]
    public void CreateAccessToken_ContainsEmailClaim()
    {
        var issuer = BuildIssuer();
        var token = issuer.CreateAccessToken(TestUser());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Contains(
            jwt.Claims,
            c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "alice@example.com"
        );
    }

    [Fact]
    public void CreateAccessToken_ContainsJtiClaim()
    {
        var issuer = BuildIssuer();
        var token = issuer.CreateAccessToken(TestUser());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Contains(
            jwt.Claims,
            c => c.Type == JwtRegisteredClaimNames.Jti && !string.IsNullOrEmpty(c.Value)
        );
    }

    [Fact]
    public void CreateAccessToken_ExpiresAccordingToOptions()
    {
        var issuer = BuildIssuer(expiryMinutes: 30);
        var before = DateTime.UtcNow;
        var token = issuer.CreateAccessToken(TestUser());
        var after = DateTime.UtcNow;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.True(jwt.ValidTo >= before.AddMinutes(30).AddSeconds(-1));
        Assert.True(jwt.ValidTo <= after.AddMinutes(30).AddSeconds(5));
    }
}
