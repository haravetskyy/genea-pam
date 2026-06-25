namespace GeneaPam.Api.Features.Auth;

public sealed record PasswordResetEmailJob(
    string To,
    string UserName,
    string LanguagePreference,
    string ResetLink,
    TimeSpan Expiry
);
