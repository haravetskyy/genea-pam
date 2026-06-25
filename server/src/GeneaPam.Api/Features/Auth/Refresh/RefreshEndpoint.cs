using ErrorOr;
using FastEndpoints;
using GeneaPam.Api.Features.Auth.Internal;
using GeneaPam.Api.Features.Auth.Login;
using GeneaPam.Api.Infrastructure.Http;
using Microsoft.Extensions.Options;

namespace GeneaPam.Api.Features.Auth.Refresh;

public sealed class RefreshEndpoint(
    ITokenIssuer tokenIssuer,
    IRefreshTokenStore refreshStore,
    IOptions<AuthOptions> authOptions
) : EndpointWithoutRequest<LoginResponse>
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
            await HttpContext.Response.SendResultAsync(AuthErrors.TokenInvalid.ToProblemResult());
            return;
        }

        var result = await RotateTokenAsync(rawToken, ct);

        await HttpContext.Response.SendResultAsync(
            result.MatchToResponse(tokens =>
            {
                AuthCookies.Append(
                    HttpContext,
                    tokens.RefreshToken,
                    authOptions.Value.RefreshTokenExpiryDays
                );
                return Results.Ok(new LoginResponse(tokens.AccessToken));
            })
        );
    }

    private async Task<ErrorOr<(string AccessToken, string RefreshToken)>> RotateTokenAsync(
        string rawToken,
        CancellationToken cancellationToken
    )
    {
        var user = await refreshStore.ValidateAndRotateAsync(rawToken, cancellationToken);
        if (user is null)
            return AuthErrors.TokenInvalid;

        var accessToken = tokenIssuer.CreateAccessToken(user);
        var newRefreshToken = await refreshStore.CreateAsync(user, cancellationToken);

        return (accessToken, newRefreshToken);
    }
}
