using Microsoft.AspNetCore.Http;

namespace PhotoGallery.ServiceDefaults.Extensions
{
    public static class HttpContextExtensions
    {
        public static string GetIpAddress(this HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                return forwardedFor.ToString().Split(',')[0].Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        public static void SetRefreshTokenCookie(this HttpContext context, string token, int expirationDays = 7)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(expirationDays)
            };

            context.Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        public static string? GetRefreshTokenFromCookie(this HttpContext context)
        {
            return context.Request.Cookies["refreshToken"];
        }

        public static void DeleteRefreshTokenCookie(this HttpContext context)
        {
            context.Response.Cookies.Delete("refreshToken");
        }
    }
}
