using GeneaPam.Api.Infrastructure.Persistence;

namespace GeneaPam.Api.Features.Auth;

public interface ITokenIssuer
{
    string CreateAccessToken(ApplicationUser user);
}
