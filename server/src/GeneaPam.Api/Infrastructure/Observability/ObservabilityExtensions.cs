namespace GeneaPam.Api.Infrastructure.Observability;

public static class ObservabilityExtensions
{
    public static IWebHostBuilder AddObservability(this IWebHostBuilder builder, IConfiguration configuration)
    {
        var dsn = configuration["SENTRY_DSN"]
            ?? throw new InvalidOperationException("SENTRY_DSN configuration is missing");

        builder.UseSentry(o =>
        {
            o.Dsn = dsn;
            o.TracesSampleRate = 1.0;
            o.SendDefaultPii = false;
        });

        return builder;
    }
}
