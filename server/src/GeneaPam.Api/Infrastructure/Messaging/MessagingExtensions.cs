using StackExchange.Redis;

namespace GeneaPam.Api.Infrastructure.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>();

        if (options is not null && !string.IsNullOrEmpty(options.ConnectionString))
        {
            services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(options.ConnectionString));
            services.AddSingleton<IMessageBroker, RedisMessageBroker>();
        }

        return services;
    }
}
