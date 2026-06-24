using GeneaPam.Api.Infrastructure.Persistence;
using GeneaPam.Api.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GeneaPam.Api.IntegrationTests.Features.Auth;

public sealed class IdentitySchemaTests(ApiFactory factory) : IntegrationTest(factory)
{
    private static readonly string[] ExpectedColumns =
    [
        "DisplayName",
        "ContactEmail",
        "IsContactEmailVisible",
        "LanguagePreference",
        "AvatarObjectKey",
    ];

    [Fact]
    public async Task AspNetUsers_HasAllCustomColumns()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var columns = await db
            .Database.SqlQueryRaw<string>(
                """
                SELECT column_name
                FROM information_schema.columns
                WHERE table_name = 'AspNetUsers'
                """
            )
            .ToListAsync();

        foreach (var expected in ExpectedColumns)
            Assert.Contains(expected, columns);
    }
}
