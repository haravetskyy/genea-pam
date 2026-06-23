namespace GeneaPam.Api.Infrastructure.Messaging;

public class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; init; } = "";
}
