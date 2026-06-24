using GeneaPam.Api.Infrastructure.Http;

namespace GeneaPam.Api.Features.Auth;

public sealed class LogoutEndpoint : IEndpoint
{
    private const string RefreshCookieName = "refresh_token";

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/logout", HandleAsync)
            .AllowAnonymous()
            .WithTags("Auth")
            .Produces(StatusCodes.Status204NoContent);
    }

    internal static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        JwtTokenService tokenService,
        CancellationToken cancellationToken
    )
    {
        var rawToken = httpContext.Request.Cookies[RefreshCookieName];

        if (!string.IsNullOrEmpty(rawToken))
            await tokenService.RevokeRefreshTokenAsync(rawToken, cancellationToken);

        httpContext.Response.Cookies.Delete(RefreshCookieName);

        return Results.NoContent();
    }
}
