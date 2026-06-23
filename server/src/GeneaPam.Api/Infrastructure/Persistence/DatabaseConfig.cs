using Npgsql;

namespace GeneaPam.Api.Infrastructure.Persistence;

public static class DatabaseConfig
{
    public static IServiceCollection AddDatabaseConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = GetConnectionString(configuration);

        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());

        return services;
    }

    public static string GetConnectionString(IConfiguration configuration)
    {
        var options = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
            ?? throw new InvalidOperationException("Database configuration is missing");

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new InvalidOperationException("Database:ConnectionString is missing or empty");

        return options.ConnectionString;
    }
}
