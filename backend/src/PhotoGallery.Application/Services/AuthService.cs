using Microsoft.AspNetCore.Identity;
using PhotoGallery.Application.DTOs.Auth;
using PhotoGallery.Application.Interfaces.Services;
using PhotoGallery.Domain.Entities;

namespace PhotoGallery.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly ITokenService _tokenService;

        public AuthService(UserManager<User> userManager, ITokenService tokenService)
        {
            _userManager = userManager;
            _tokenService = tokenService;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, string role = "User", CancellationToken cancellationToken = default)
        {
            var existingUserByEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUserByEmail != null)
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            var existingUserByUsername = await _userManager.FindByNameAsync(dto.UserName);
            if (existingUserByUsername != null)
            {
                throw new InvalidOperationException("User with this username already exists");
            }

            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create user: {errors}");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                throw new InvalidOperationException("Failed to assign role to user");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(user, roles);

            return new AuthResponseDto
            {
                AccessToken = accessToken
            };
        }

        public async Task<(AuthResponseDto response, string? refreshToken)> LoginAsync(LoginDto dto, string ipAddress, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
                throw new UnauthorizedAccessException("Invalid email or password");

            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id, ipAddress);

            return (new AuthResponseDto { AccessToken = accessToken }, refreshToken.Token);
        }

        public async Task<(AuthResponseDto response, string refreshToken)> RefreshTokenAsync(string token, string ipAddress, CancellationToken cancellationToken = default)
        {
            var refreshToken = await _tokenService.GetRefreshTokenAsync(token);

            if (refreshToken == null)
                throw new UnauthorizedAccessException("Invalid refresh token");

            if (!refreshToken.IsActive)
                throw new UnauthorizedAccessException("Refresh token is expired or revoked");

            var user = refreshToken.User;

            await _tokenService.RevokeTokenAsync(token, ipAddress, "Replaced by new token");
            var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync(
                user.Id,
                ipAddress,
                token
            );

            var roles = await _userManager.GetRolesAsync(user);
            var accessToken = _tokenService.GenerateAccessToken(user, roles);

            var response = new AuthResponseDto
            {
                AccessToken = accessToken
            };

            return (response, newRefreshToken.Token);
        }
    }
}