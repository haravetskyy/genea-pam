using Microsoft.AspNetCore.Identity;

namespace GeneaPam.Api.Infrastructure.Persistence;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public bool IsContactEmailVisible { get; set; } = false;
    public string LanguagePreference { get; set; } = "en";
    public string? AvatarObjectKey { get; set; }
}
