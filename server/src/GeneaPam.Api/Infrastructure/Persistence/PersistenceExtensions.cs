using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GeneaPam.Api.Infrastructure.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
            ?? throw new InvalidOperationException("Database configuration is missing");

        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.AddSingleton(_ => new NpgsqlDataSourceBuilder(options.ConnectionString).Build());
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(options.ConnectionString));

        return services;
    }
}
