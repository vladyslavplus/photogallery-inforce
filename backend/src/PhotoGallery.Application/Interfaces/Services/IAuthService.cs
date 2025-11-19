using PhotoGallery.Application.DTOs.Auth;

namespace PhotoGallery.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto, string role = "User", CancellationToken cancellationToken = default);
        Task<(AuthResponseDto response, string? refreshToken)> LoginAsync(LoginDto dto, string ipAddress, CancellationToken cancellationToken = default);
        Task<(AuthResponseDto response, string refreshToken)> RefreshTokenAsync(string token, string ipAddress, CancellationToken cancellationToken = default);
    }
}
