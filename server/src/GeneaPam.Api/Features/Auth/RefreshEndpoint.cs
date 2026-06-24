using ErrorOr;
using GeneaPam.Api.Infrastructure.Http;

namespace GeneaPam.Api.Features.Auth;

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
        CancellationToken cancellationToken
    )
    {
        var rawToken = AuthCookies.Read(httpContext);
        if (string.IsNullOrEmpty(rawToken))
            return AuthErrors.TokenInvalid.ToProblemResult();

        var result = await RotateTokenAsync(rawToken, tokenIssuer, refreshStore, cancellationToken);

        return result.MatchToResponse(response =>
        {
            AuthCookies.Append(httpContext, response.RefreshToken);
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
