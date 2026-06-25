using ErrorOr;
using GeneaPam.Api.Features.Auth.Internal;
using GeneaPam.Api.Features.Auth.Login;
using GeneaPam.Api.Infrastructure.Http;
using Microsoft.Extensions.Options;

namespace GeneaPam.Api.Features.Auth.Refresh;

public sealed class RefreshEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/refresh", HandleAsync)
            .AllowAnonymous()
            .WithTags("Auth")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    internal static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        ITokenIssuer tokenIssuer,
        IRefreshTokenStore refreshStore,
        IOptions<AuthOptions> authOptions,
        CancellationToken cancellationToken
    )
    {
        var rawToken = AuthCookies.Read(httpContext);
        if (string.IsNullOrEmpty(rawToken))
            return AuthErrors.TokenInvalid.ToProblemResult();

        var result = await RotateTokenAsync(rawToken, tokenIssuer, refreshStore, cancellationToken);

        return result.MatchToResponse(response =>
        {
            AuthCookies.Append(
                httpContext,
                response.RefreshToken,
                authOptions.Value.RefreshTokenExpiryDays
            );
            return Results.Ok(new LoginResponse(response.AccessToken));
        });
    }

    private static async Task<ErrorOr<(string AccessToken, string RefreshToken)>> RotateTokenAsync(
        string rawToken,
        ITokenIssuer tokenIssuer,
        IRefreshTokenStore refreshStore,
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
