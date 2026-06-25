namespace GeneaPam.Api.Infrastructure.Cache;

public class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; init; } = "";
}
