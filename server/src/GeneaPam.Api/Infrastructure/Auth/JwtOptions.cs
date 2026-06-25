namespace GeneaPam.Api.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Auth";
    public string JwtSecret { get; init; } = string.Empty;
}
