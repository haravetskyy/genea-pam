using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GeneaPam.Api.Infrastructure.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDatabaseConfig(configuration);
        services.AddDbContext<AppDbContext>((sp, o) => o.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>()));

        return services;
    }
}
