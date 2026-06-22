using GeneaPam.Api.Infrastructure.Persistence;
using Wolverine;
using Wolverine.Postgresql;

namespace GeneaPam.Api.Infrastructure.Jobs;

public static class JobsExtensions
{
    public static IHostBuilder AddJobs(this IHostBuilder host, IConfiguration configuration)
    {
        var dbOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
            ?? throw new InvalidOperationException("Database configuration is missing");

        host.UseWolverine(opts =>
        {
            opts.PersistMessagesWithPostgresql(dbOptions.ConnectionString);
            opts.Policies.AutoApplyTransactions();
        });

        host.ConfigureServices(services =>
            services.AddSingleton<IJobDispatcher, WolverineJobDispatcher>());

        return host;
    }
}
