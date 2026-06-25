using GeneaPam.Api.Features.Auth.Register;
using GeneaPam.Api.Infrastructure.Jobs;
using GeneaPam.Api.Infrastructure.Persistence;
using GeneaPam.Api.IntegrationTests.Infrastructure.Adapters;
using GeneaPam.Api.UnitTests.Infrastructure.Adapters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using WireMock.Server;

namespace GeneaPam.Api.IntegrationTests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public WireMockServer WireMock { get; } = WireMockServer.Start();
    public FakeDnsResolver DnsResolver { get; } = new FakeDnsResolver();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:ConnectionString", _postgres.GetConnectionString());
        builder.UseSetting("Auth:HibpBaseUrl", WireMock.Url!);
        builder.UseSetting("Auth:JwtSecret", "test-secret-key-that-is-at-least-32-chars!!");

        builder.ConfigureServices(services =>
        {
            foreach (
                var descriptor in services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>))
                    .ToList()
            )
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(o => o.UseNpgsql(_postgres.GetConnectionString()));

            services.AddSingleton<InMemoryJobDispatcher>();
            services.AddSingleton<IJobDispatcher>(sp =>
                sp.GetRequiredService<InMemoryJobDispatcher>()
            );
            services.AddSingleton<IDnsResolver>(DnsResolver);
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
