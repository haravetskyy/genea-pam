namespace GeneaPam.Api.Infrastructure.Persistence;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; init; } = "";
}
