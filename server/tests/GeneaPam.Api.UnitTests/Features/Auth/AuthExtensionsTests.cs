using GeneaPam.Api.Features.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GeneaPam.Api.UnitTests.Features.Auth;

public sealed class AuthExtensionsTests
{
    private static IConfiguration BuildConfig(string? jwtSecret) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["Auth:JwtSecret"] = jwtSecret }
            )
            .Build();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("only-31-chars-long-here-x-x-x-x")]
    public void AddAuthTokens_Throws_WhenJwtSecretWeakInProduction(string? secret)
    {
        var config = BuildConfig(secret);

        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddAuthTokens(config, isProduction: true)
        );
    }

    [Fact]
    public void AddAuthTokens_DoesNotThrow_WhenJwtSecretWeakInDevelopment()
    {
        var config = BuildConfig("");

        var ex = Record.Exception(() =>
            new ServiceCollection().AddAuthTokens(config, isProduction: false)
        );

        Assert.Null(ex);
    }

    [Fact]
    public void AddAuthTokens_DoesNotThrow_WhenJwtSecretStrongInProduction()
    {
        var config = BuildConfig("test-secret-key-that-is-at-least-32-chars!!");

        var ex = Record.Exception(() =>
            new ServiceCollection().AddAuthTokens(config, isProduction: true)
        );

        Assert.Null(ex);
    }
}
