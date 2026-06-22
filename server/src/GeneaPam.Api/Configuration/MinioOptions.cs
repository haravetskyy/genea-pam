namespace GeneaPam.Api.Configuration;

public class MinioOptions
{
    public const string SectionName = "Minio";

    public string Endpoint { get; init; } = "";
    public string AccessKey { get; init; } = "";
    public string SecretKey { get; init; } = "";
}
