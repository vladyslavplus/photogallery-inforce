using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhotoGallery.Application.DTOs.Auth;
using PhotoGallery.Application.Interfaces.Services;
using PhotoGallery.ServiceDefaults.Extensions;

namespace PhotoGallery.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;

        public AuthController(IAuthService authService, ITokenService tokenService)
        {
            _authService = authService;
            _tokenService = tokenService;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken cancellationToken)
        {
            var response = await _authService.RegisterAsync(dto, cancellationToken: cancellationToken);
            return Ok(response);
        }

        /// <summary>
        /// Login with email and password
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
        {
            var ipAddress = HttpContext.GetIpAddress();
            var (response, refreshToken) = await _authService.LoginAsync(dto, ipAddress, cancellationToken);

            if (!string.IsNullOrEmpty(refreshToken))
                HttpContext.SetRefreshTokenCookie(refreshToken);

            return Ok(response);
        }

        /// <summary>
        /// Refresh access token using refresh token from cookie
        /// </summary>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AuthResponseDto), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
        {
            var refreshToken = HttpContext.GetRefreshTokenFromCookie();
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized(new { message = "Refresh token not found in cookies" });

            var ipAddress = HttpContext.GetIpAddress();
            var (response, newRefreshToken) = await _authService.RefreshTokenAsync(refreshToken, ipAddress, cancellationToken);

            HttpContext.SetRefreshTokenCookie(newRefreshToken);

            return Ok(response);
        }

        /// <summary>
        /// Logout and revoke current refresh token
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var refreshToken = HttpContext.GetRefreshTokenFromCookie();
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var ipAddress = HttpContext.GetIpAddress();
                await _tokenService.RevokeTokenAsync(refreshToken, ipAddress, cancellationToken: cancellationToken);
            }

            HttpContext.DeleteRefreshTokenCookie();
            return Ok(new { message = "Logged out successfully" });
        }

        /// <summary>
        /// Revoke all active refresh tokens for current user
        /// </summary>
        [Authorize]
        [HttpPost("revoke-all")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> RevokeAll(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var ipAddress = HttpContext.GetIpAddress();
            var revokedCount = await _tokenService.RevokeAllUserTokensAsync(userId, ipAddress, cancellationToken);

            HttpContext.DeleteRefreshTokenCookie();

            return Ok(new
            {
                message = revokedCount == 0 ? "No active tokens to revoke" : "All tokens revoked successfully",
                revokedCount
            });
        }
    }
}