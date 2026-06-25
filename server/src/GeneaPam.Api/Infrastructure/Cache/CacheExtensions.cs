using StackExchange.Redis;

namespace GeneaPam.Api.Infrastructure.Cache;

public static class CacheExtensions
{
    public static IServiceCollection AddCache(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var options = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>();

        if (options is not null && !string.IsNullOrEmpty(options.ConnectionString))
        {
            services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(options.ConnectionString)
            );
        }

        return services;
    }
}
