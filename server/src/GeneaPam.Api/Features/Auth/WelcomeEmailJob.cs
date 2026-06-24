using GeneaPam.Api.Infrastructure.Email;

namespace GeneaPam.Api.Features.Auth;

public sealed record WelcomeEmailJob(string To, string UserName, string LanguagePreference)
    : IEmailJob;
