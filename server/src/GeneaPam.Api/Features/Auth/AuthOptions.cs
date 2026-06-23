namespace GeneaPam.Api.Features.Auth;

public class AuthOptions
{
    public const string SectionName = "Auth";

    public string HibpBaseUrl { get; init; } = "https://api.pwnedpasswords.com/";
}
