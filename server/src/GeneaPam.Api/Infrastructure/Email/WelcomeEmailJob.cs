namespace GeneaPam.Api.Infrastructure.Email;

public sealed record WelcomeEmailJob(string To, string UserName, string LanguagePreference)
    : IEmailJob;
