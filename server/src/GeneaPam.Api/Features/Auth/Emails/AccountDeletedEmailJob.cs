namespace GeneaPam.Api.Features.Auth.Emails;

public sealed record AccountDeletedEmailJob(string To, string UserName, string LanguagePreference);
