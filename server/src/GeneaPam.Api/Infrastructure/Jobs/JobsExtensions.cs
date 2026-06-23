using GeneaPam.Api.Infrastructure.Persistence;
using Wolverine;
using Wolverine.Postgresql;

namespace GeneaPam.Api.Infrastructure.Jobs;

public static class JobsExtensions
{
    public static IHostBuilder AddJobs(this IHostBuilder host, IConfiguration configuration)
    {
        var connectionString = DatabaseConfig.GetConnectionString(configuration);

        host.UseWolverine(opts =>
        {
            opts.PersistMessagesWithPostgresql(connectionString);
            opts.Policies.AutoApplyTransactions();
        });

        host.ConfigureServices(services =>
            services.AddScoped<IJobDispatcher, WolverineJobDispatcher>());

        return host;
    }
}
