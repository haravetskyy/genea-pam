using GeneaPam.Api.Infrastructure.Persistence;

namespace GeneaPam.Api.Features.Auth;

public interface IRefreshTokenStore
{
    Task<string> CreateAsync(ApplicationUser user, CancellationToken cancellationToken);
    Task<ApplicationUser?> ValidateAndRotateAsync(
        string rawToken,
        CancellationToken cancellationToken
    );
    Task RevokeAsync(string rawToken, CancellationToken cancellationToken);
}
