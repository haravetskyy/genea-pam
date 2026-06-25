namespace GeneaPam.Api.Features.Auth;

public sealed record WelcomeEmailJob(string To, string UserName, string LanguagePreference);
