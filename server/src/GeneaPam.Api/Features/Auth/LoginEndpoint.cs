using ErrorOr;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GeneaPam.Api.Features.Auth;

public sealed class LoginEndpoint : IEndpoint
{
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
        ITokenIssuer tokenIssuer,
        IRefreshTokenStore refreshStore,
        IOptions<AuthOptions> authOptions,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var result = await AuthenticateAsync(
            request,
            userManager,
            tokenIssuer,
            refreshStore,
            cancellationToken
        );

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

    private static async Task<ErrorOr<(string AccessToken, string RefreshToken)>> AuthenticateAsync(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        ITokenIssuer tokenIssuer,
        IRefreshTokenStore refreshStore,
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
