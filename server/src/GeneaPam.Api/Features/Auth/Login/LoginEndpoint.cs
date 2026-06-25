using ErrorOr;
using FastEndpoints;
using GeneaPam.Api.Features.Auth.Internal;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GeneaPam.Api.Features.Auth.Login;

public sealed class LoginEndpoint(
    UserManager<ApplicationUser> userManager,
    ITokenIssuer tokenIssuer,
    IRefreshTokenStore refreshStore,
    IOptions<AuthOptions> authOptions
) : Endpoint<LoginRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();
        Tags("Auth");
        Description(b =>
            b.Produces<LoginResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
        );
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var result = await AuthenticateAsync(req, ct);

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

    private async Task<ErrorOr<(string AccessToken, string RefreshToken)>> AuthenticateAsync(
        LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return AuthErrors.InvalidCredentials;

        var valid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!valid)
            return AuthErrors.InvalidCredentials;

        var accessToken = tokenIssuer.CreateAccessToken(user);
        var refreshToken = await refreshStore.CreateAsync(user, cancellationToken);

        return (accessToken, refreshToken);
    }
}
