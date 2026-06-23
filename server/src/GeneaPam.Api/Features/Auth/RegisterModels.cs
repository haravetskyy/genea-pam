namespace GeneaPam.Api.Features.Auth;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);

public sealed record RegisterResponse(string UserId, string DisplayName);
