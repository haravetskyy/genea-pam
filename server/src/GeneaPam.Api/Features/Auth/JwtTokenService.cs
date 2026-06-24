using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GeneaPam.Api.Features.Auth;

public sealed class JwtTokenService(
    IOptions<AuthOptions> options,
    AppDbContext db,
    UserManager<ApplicationUser> userManager
)
{
    private readonly AuthOptions _options = options.Value;

    public string CreateAccessToken(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.JwtExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> CreateRefreshTokenAsync(
        ApplicationUser user,
        CancellationToken cancellationToken
    )
    {
        var rawToken = GenerateRawToken();
        var hash = HashToken(rawToken);

        db.RefreshTokens.Add(
            new RefreshToken
            {
                UserId = user.Id,
                TokenHash = hash,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenExpiryDays),
            }
        );

        await db.SaveChangesAsync(cancellationToken);
        return rawToken;
    }

    public async Task<ApplicationUser?> ValidateRefreshTokenAsync(
        string rawToken,
        CancellationToken cancellationToken
    )
    {
        var hash = HashToken(rawToken);
        var stored = await db
            .RefreshTokens.Where(t => t.TokenHash == hash)
            .FirstOrDefaultAsync(cancellationToken);

        if (stored is null || stored.IsUsed || stored.ExpiresAt <= DateTimeOffset.UtcNow)
            return null;

        stored.IsUsed = true;
        await db.SaveChangesAsync(cancellationToken);

        return await userManager.FindByIdAsync(stored.UserId);
    }

    public async Task RevokeRefreshTokenAsync(string rawToken, CancellationToken cancellationToken)
    {
        var hash = HashToken(rawToken);
        var stored = await db
            .RefreshTokens.Where(t => t.TokenHash == hash)
            .FirstOrDefaultAsync(cancellationToken);

        if (stored is null || stored.IsUsed)
            return;

        stored.IsUsed = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateRawToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
