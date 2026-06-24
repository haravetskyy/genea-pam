namespace GeneaPam.Api.Features.Auth;

internal static class AuthCookies
{
    internal const string CookieName = "refresh_token";

    private static CookieOptions LiveOptions() =>
        new()
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(30),
        };

    internal static void Append(HttpContext ctx, string refreshToken) =>
        ctx.Response.Cookies.Append(CookieName, refreshToken, LiveOptions());

    internal static void Delete(HttpContext ctx) => ctx.Response.Cookies.Delete(CookieName);

    internal static string? Read(HttpContext ctx) => ctx.Request.Cookies[CookieName];
}
