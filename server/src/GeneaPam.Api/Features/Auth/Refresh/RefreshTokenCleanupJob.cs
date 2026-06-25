using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Features.Auth.Refresh;

public sealed class RefreshTokenCleanupJob(AppDbContext db, ILogger<RefreshTokenCleanupJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var deleted = await db
            .RefreshTokens.Where(t => t.IsUsed || t.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);

        logger.LogInformation("RefreshTokenCleanupJob deleted {Count} token(s).", deleted);
    }
}
