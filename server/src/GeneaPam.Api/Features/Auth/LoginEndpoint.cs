using ErrorOr;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace GeneaPam.Api.Features.Auth;

public sealed class LoginEndpoint : IEndpoint
{
    private const string RefreshCookieName = "refresh_token";

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", HandleAsync)
            .AllowAnonymous()
            .WithTags("Auth")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    internal static async Task<IResult> HandleAsync(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        JwtTokenService tokenService,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await AuthenticateAsync(request, userManager, tokenService, cancellationToken);

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

    private static async Task<ErrorOr<(string AccessToken, string RefreshToken)>> AuthenticateAsync(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        JwtTokenService tokenService,
        CancellationToken cancellationToken
    )
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return AuthErrors.InvalidCredentials;

        var valid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!valid)
            return AuthErrors.InvalidCredentials;

        var accessToken = tokenService.CreateAccessToken(user);
        var refreshToken = await tokenService.CreateRefreshTokenAsync(user, cancellationToken);

        return (accessToken, refreshToken);
    }
}
