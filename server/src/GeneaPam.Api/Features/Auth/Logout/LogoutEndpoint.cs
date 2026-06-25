using GeneaPam.Api.Features.Auth.Internal;
using GeneaPam.Api.Infrastructure.Http;

namespace GeneaPam.Api.Features.Auth.Logout;

public sealed class LogoutEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/logout", HandleAsync)
            .AllowAnonymous()
            .WithTags("Auth")
            .Produces(StatusCodes.Status204NoContent);
    }

    internal static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        IRefreshTokenStore refreshStore,
        CancellationToken cancellationToken
    )
    {
        var rawToken = AuthCookies.Read(httpContext);

        if (!string.IsNullOrEmpty(rawToken))
            await refreshStore.RevokeAsync(rawToken, cancellationToken);

        AuthCookies.Delete(httpContext);

        return Results.NoContent();
    }
}
