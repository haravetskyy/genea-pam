using GeneaPam.Api.Infrastructure.Persistence;
using JasperFx.CodeGeneration;
using Wolverine;
using Wolverine.Configuration;
using Wolverine.ErrorHandling;
using Wolverine.Postgresql;
using Wolverine.Runtime;
using Wolverine.Runtime.Handlers;

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
            opts.Policies.Add<RetryAndDeadLetterPolicy>();
        });

        host.ConfigureServices(services =>
            services.AddScoped<IJobDispatcher, WolverineJobDispatcher>());

        return host;
    }
}

internal sealed class RetryAndDeadLetterPolicy : IHandlerPolicy
{
    public void Apply(IReadOnlyList<HandlerChain> chains, GenerationRules rules, IServiceContainer container)
    {
        foreach (var chain in chains)
        {
            chain.OnAnyException()
                .RetryWithCooldown(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
        }
    }
}
