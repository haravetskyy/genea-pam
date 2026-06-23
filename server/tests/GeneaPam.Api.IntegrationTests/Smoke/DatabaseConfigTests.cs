using GeneaPam.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace GeneaPam.Api.IntegrationTests.Smoke;

public sealed class DatabaseConfigTests(ApiFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public void NpgsqlDataSource_IsRegisteredAndResolvable()
    {
        using var scope = factory.Services.CreateScope();
        var dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();

        Assert.NotNull(dataSource);
    }

    [Fact]
    public void MissingConnectionString_ThrowsInvalidOperationExceptionAtStartup()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var app = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseSetting("Database:ConnectionString", "");
                    builder.UseSetting("SENTRY_DSN", "https://fake@sentry.io/0");
                });

            _ = app.Services;
        });

        Assert.Contains("Database", ex.Message);
    }
}
