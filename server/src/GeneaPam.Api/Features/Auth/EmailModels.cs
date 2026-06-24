namespace GeneaPam.Api.Features.Auth;

public sealed record WelcomeEmailModel(string UserName);

public sealed record PasswordResetEmailModel(string UserName, string ResetLink, int ExpiryHours);

public sealed record AccountDeletedEmailModel(string UserName);
