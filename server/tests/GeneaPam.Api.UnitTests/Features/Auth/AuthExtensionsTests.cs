using GeneaPam.Api.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GeneaPam.Api.UnitTests.Features.Auth;

public sealed class AuthExtensionsTests
{
    private static IConfiguration BuildConfig(string? jwtSecret) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["Auth:JwtSecret"] = jwtSecret }
            )
            .Build();

    private sealed class FakeEnvironment(bool isDevelopment) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } =
            isDevelopment ? Environments.Development : Environments.Production;
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = "/";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("only-31-chars-long-here-x-x-x-x")]
    public void AddJwtBearer_Throws_WhenJwtSecretWeakInProduction(string? secret)
    {
        var config = BuildConfig(secret);

        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddJwtBearer(config, new FakeEnvironment(isDevelopment: false))
        );
    }

    [Fact]
    public void AddJwtBearer_DoesNotThrow_WhenJwtSecretWeakInDevelopment()
    {
        var config = BuildConfig("");

        var ex = Record.Exception(() =>
            new ServiceCollection().AddJwtBearer(config, new FakeEnvironment(isDevelopment: true))
        );

        Assert.Null(ex);
    }

    [Fact]
    public void AddJwtBearer_DoesNotThrow_WhenJwtSecretStrongInProduction()
    {
        var config = BuildConfig("test-secret-key-that-is-at-least-32-chars!!");

        var ex = Record.Exception(() =>
            new ServiceCollection().AddJwtBearer(config, new FakeEnvironment(isDevelopment: false))
        );

        Assert.Null(ex);
    }
}
