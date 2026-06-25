namespace GeneaPam.Api.Features.Auth.Internal;

public class AuthOptions
{
    public const string SectionName = "Auth";

    public string HibpBaseUrl { get; init; } = "https://api.pwnedpasswords.com/";

    public string JwtSecret { get; init; } = string.Empty;
    public int JwtExpiryMinutes { get; init; } = 15;
    public int RefreshTokenExpiryDays { get; init; } = 30;
    public int CleanupIntervalHours { get; init; } = 24;
}
