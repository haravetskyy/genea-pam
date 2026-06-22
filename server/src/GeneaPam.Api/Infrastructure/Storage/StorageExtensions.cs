using Minio;

namespace GeneaPam.Api.Infrastructure.Storage;

public static class StorageExtensions
{
    public static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>()
            ?? throw new InvalidOperationException("Minio configuration is missing");

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.AddMinio(c => c
            .WithEndpoint(options.Endpoint)
            .WithCredentials(options.AccessKey, options.SecretKey)
            .Build());

        return services;
    }
}
