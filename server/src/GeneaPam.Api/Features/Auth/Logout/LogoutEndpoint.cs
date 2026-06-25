using FastEndpoints;
using GeneaPam.Api.Features.Auth.Internal;

namespace GeneaPam.Api.Features.Auth.Logout;

public sealed class LogoutEndpoint(IRefreshTokenStore refreshStore) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/auth/logout");
        AllowAnonymous();
        Tags("Auth");
        Description(b => b.Produces(StatusCodes.Status204NoContent));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var rawToken = AuthCookies.Read(HttpContext);

        if (!string.IsNullOrEmpty(rawToken))
            await refreshStore.RevokeAsync(rawToken, ct);

        AuthCookies.Delete(HttpContext);

        await HttpContext.Response.SendNoContentAsync(ct);
    }
}
