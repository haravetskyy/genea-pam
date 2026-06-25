namespace GeneaPam.Api.Features.Auth;

public sealed record AccountDeletedEmailJob(string To, string UserName, string LanguagePreference);
