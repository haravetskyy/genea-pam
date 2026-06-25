using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using GeneaPam.Api.Features.Auth.Refresh;
using GeneaPam.Api.Infrastructure.Persistence;
using GeneaPam.Api.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace GeneaPam.Api.IntegrationTests.Features.Auth.Refresh;

public sealed class RefreshTokenCleanupJobTests(ApiFactory factory) : IntegrationTest(factory)
{
    private const string SafePassword = "SafeP@ss!99xyz";

    private static string HibpPrefix(string password)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(password));
        return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant()[..5];
    }

    private async Task<string> EnsureUserExistsAsync(string email)
    {
        var prefix = HibpPrefix(SafePassword);
        WireMock
            .Given(Request.Create().WithPath($"/range/{prefix}").UsingGet())
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithBody("0000000000000000000000000000000000001:0\r\n")
            );
        await Client.PostAsJsonAsync(
            "/auth/register",
            new
            {
                email,
                password = SafePassword,
                displayName = "Test User",
            }
        );

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == email);
        return user.Id;
    }

    [Fact]
    public async Task CleanupJob_DeletesExpiredTokens()
    {
        var userId = await EnsureUserExistsAsync("cleanup_expired@gmail.com");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = scope.ServiceProvider.GetRequiredService<RefreshTokenCleanupJob>();

        db.RefreshTokens.AddRange(
            new RefreshToken
            {
                UserId = userId,
                TokenHash = "cleanup-expired-hash-1",
                IsUsed = false,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
            },
            new RefreshToken
            {
                UserId = userId,
                TokenHash = "cleanup-valid-hash-1",
                IsUsed = false,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            }
        );
        await db.SaveChangesAsync();

        await job.RunAsync(CancellationToken.None);

        Assert.False(await db.RefreshTokens.AnyAsync(t => t.TokenHash == "cleanup-expired-hash-1"));
        Assert.True(await db.RefreshTokens.AnyAsync(t => t.TokenHash == "cleanup-valid-hash-1"));
    }

    [Fact]
    public async Task CleanupJob_DeletesUsedTokens()
    {
        var userId = await EnsureUserExistsAsync("cleanup_used@gmail.com");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = scope.ServiceProvider.GetRequiredService<RefreshTokenCleanupJob>();

        db.RefreshTokens.AddRange(
            new RefreshToken
            {
                UserId = userId,
                TokenHash = "cleanup-used-hash-1",
                IsUsed = true,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            },
            new RefreshToken
            {
                UserId = userId,
                TokenHash = "cleanup-active-hash-1",
                IsUsed = false,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            }
        );
        await db.SaveChangesAsync();

        await job.RunAsync(CancellationToken.None);

        Assert.False(await db.RefreshTokens.AnyAsync(t => t.TokenHash == "cleanup-used-hash-1"));
        Assert.True(await db.RefreshTokens.AnyAsync(t => t.TokenHash == "cleanup-active-hash-1"));
    }
}
