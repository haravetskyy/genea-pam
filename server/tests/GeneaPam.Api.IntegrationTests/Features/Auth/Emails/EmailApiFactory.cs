using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using WireMock.Server;

namespace GeneaPam.Api.IntegrationTests.Features.Auth.Emails;

public sealed class EmailApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public WireMockServer WireMock { get; } = WireMockServer.Start();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:ConnectionString", _postgres.GetConnectionString());
        builder.UseSetting("Email:BaseUrl", WireMock.Url!);
        builder.UseSetting("Email:ApiKey", "test-key");

        builder.ConfigureServices(services =>
        {
            foreach (
                var descriptor in services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>))
                    .ToList()
            )
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(o => o.UseNpgsql(_postgres.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        WireMock.Stop();
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
