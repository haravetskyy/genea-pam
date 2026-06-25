using ErrorOr;
using FastEndpoints;
using GeneaPam.Api.Features.Auth.Internal;
using GeneaPam.Api.Features.Auth.Login;
using Microsoft.Extensions.Options;

namespace GeneaPam.Api.Features.Auth.Refresh;

public sealed class RefreshEndpoint(
    ITokenIssuer tokenIssuer,
    IRefreshTokenStore refreshStore,
    IOptions<AuthOptions> authOptions
) : EndpointWithoutRequest<ErrorOr<LoginResponse>>
{
    public override void Configure()
    {
        Post("/auth/refresh");
        AllowAnonymous();
        Tags("Auth");
        Description(b =>
            b.Produces<LoginResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
        );
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var rawToken = AuthCookies.Read(HttpContext);
        if (string.IsNullOrEmpty(rawToken))
        {
            Response = AuthErrors.TokenInvalid;
            return;
        }

        var result = await RotateTokenAsync(rawToken, ct);

        Response = result.Then(tokens =>
        {
            AuthCookies.Append(
                HttpContext,
                tokens.RefreshToken,
                authOptions.Value.RefreshTokenExpiryDays
            );
            return new LoginResponse(tokens.AccessToken);
        });
    }

    private async Task<ErrorOr<AuthTokenPair>> RotateTokenAsync(
        string rawToken,
        CancellationToken cancellationToken
    )
    {
        var user = await refreshStore.ValidateAndRotateAsync(rawToken, cancellationToken);
        if (user is null)
            return AuthErrors.TokenInvalid;

        var accessToken = tokenIssuer.CreateAccessToken(user);
        var newRefreshToken = await refreshStore.CreateAsync(user, cancellationToken);

        return new AuthTokenPair(accessToken, newRefreshToken);
    }
}
