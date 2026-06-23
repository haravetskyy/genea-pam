namespace GeneaPam.Api.Infrastructure.Email;

public sealed record AccountDeletedEmailJob(
    string To,
    string UserName,
    string LanguagePreference) : IEmailJob;
