using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PhotoGallery.ServiceDefaults.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal principal)
        {
            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user ID in token");
            }

            return userId;
        }

        public static string GetUserName(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
                ?? principal.FindFirst(ClaimTypes.Name)?.Value
                ?? throw new UnauthorizedAccessException("User name not found in token");
        }

        public static string GetUserEmail(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                ?? principal.FindFirst(ClaimTypes.Email)?.Value
                ?? throw new UnauthorizedAccessException("Email not found in token");
        }

        public static IEnumerable<string> GetUserRoles(this ClaimsPrincipal principal)
        {
            return principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
        }

        public static bool IsInRole(this ClaimsPrincipal principal, string role)
        {
            return principal.FindAll(ClaimTypes.Role).Any(c => c.Value == role);
        }
    }
}
