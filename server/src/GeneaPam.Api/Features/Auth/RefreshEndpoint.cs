using ErrorOr;
using GeneaPam.Api.Infrastructure.Http;

namespace GeneaPam.Api.Features.Auth;

public sealed class RefreshEndpoint : IEndpoint
{
    private const string RefreshCookieName = "refresh_token";

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
        var rawToken = httpContext.Request.Cookies[RefreshCookieName];
        if (string.IsNullOrEmpty(rawToken))
            return AuthErrors.TokenInvalid.ToProblemResult();

        var result = await RotateTokenAsync(rawToken, tokenIssuer, refreshStore, cancellationToken);

        return result.MatchToResponse(response =>
        {
            httpContext.Response.Cookies.Append(
                RefreshCookieName,
                response.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                }
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
