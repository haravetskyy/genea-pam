namespace GeneaPam.Api.Infrastructure.Storage;

public class StorageOptions
{
    public const string SectionName = "Minio";

    public string Endpoint { get; init; } = "";
    public string AccessKey { get; init; } = "";
    public string SecretKey { get; init; } = "";
}
