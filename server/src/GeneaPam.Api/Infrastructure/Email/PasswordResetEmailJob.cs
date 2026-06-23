namespace GeneaPam.Api.Infrastructure.Email;

public sealed record PasswordResetEmailJob(
    string To,
    string UserName,
    string LanguagePreference,
    string ResetLink,
    TimeSpan Expiry) : IEmailJob;
