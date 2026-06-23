namespace GeneaPam.Api.Infrastructure.Email;

public sealed class ResendOptions
{
    public const string SectionName = "Email";

    public string BaseUrl { get; set; } = "https://api.resend.com";
    public string ApiKey { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "noreply@genea.app";
}
