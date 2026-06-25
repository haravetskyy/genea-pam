namespace GeneaPam.Api.Features.Auth.Emails;

public sealed record WelcomeEmailJob(string To, string UserName, string LanguagePreference);
