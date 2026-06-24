using System.Security.Cryptography;
using System.Text;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GeneaPam.Api.Features.Auth;

public sealed class DbRefreshTokenStore(
    IOptions<AuthOptions> options,
    AppDbContext db,
    UserManager<ApplicationUser> userManager
) : IRefreshTokenStore
{
    private readonly AuthOptions _options = options.Value;

    public async Task<string> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
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

    public async Task<ApplicationUser?> ValidateAndRotateAsync(
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

    public async Task RevokeAsync(string rawToken, CancellationToken cancellationToken)
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

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
