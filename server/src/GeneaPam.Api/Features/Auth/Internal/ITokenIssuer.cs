using GeneaPam.Api.Infrastructure.Persistence;

namespace GeneaPam.Api.Features.Auth.Internal;

public interface ITokenIssuer
{
    string CreateAccessToken(ApplicationUser user);
}
