namespace GeneaPam.Api.Configuration;

public class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; init; } = "";
}
