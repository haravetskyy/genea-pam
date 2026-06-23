namespace GeneaPam.Api.Infrastructure.Observability;

public static class ObservabilityExtensions
{
    public static IWebHostBuilder AddObservability(
        this IWebHostBuilder builder,
        IConfiguration configuration
    )
    {
        var dsn = configuration["SENTRY_DSN"];

        if (!string.IsNullOrEmpty(dsn))
        {
            builder.UseSentry(o =>
            {
                o.Dsn = dsn;
                o.TracesSampleRate = 1.0;
                o.SendDefaultPii = false;
            });
        }

        builder.ConfigureServices(services =>
            services.AddSingleton<IObservabilityAdapter, SentryObservabilityAdapter>()
        );

        return builder;
    }
}
