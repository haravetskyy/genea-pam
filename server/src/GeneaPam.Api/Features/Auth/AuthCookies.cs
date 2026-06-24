namespace GeneaPam.Api.Features.Auth;

internal static class AuthCookies
{
    internal const string CookieName = "refresh_token";

    private static CookieOptions LiveOptions(int expiryDays) =>
        new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(expiryDays),
        };

    internal static void Append(HttpContext ctx, string refreshToken, int expiryDays) =>
        ctx.Response.Cookies.Append(CookieName, refreshToken, LiveOptions(expiryDays));

    internal static void Delete(HttpContext ctx) => ctx.Response.Cookies.Delete(CookieName);

    internal static string? Read(HttpContext ctx) => ctx.Request.Cookies[CookieName];
}
