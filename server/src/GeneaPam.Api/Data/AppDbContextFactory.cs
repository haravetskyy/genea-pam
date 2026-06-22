using GeneaPam.Api.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GeneaPam.Api.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<AppDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var dbOptions = config.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
            ?? throw new InvalidOperationException("Database configuration is missing");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(dbOptions.ConnectionString)
            .Options;

        return new AppDbContext(options);
    }
}
