using PhotoGallery.Domain.Entities;

namespace PhotoGallery.Application.Interfaces.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user, IList<string> roles);
        Task<RefreshToken> GenerateRefreshTokenAsync(Guid userId, string ipAddress, string? replacedToken = null, CancellationToken cancellationToken = default);
        Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
        Task RevokeTokenAsync(string token, string ipAddress, string reason = "Revoked by user", CancellationToken cancellationToken = default);
        Task<int> RevokeAllUserTokensAsync(Guid userId, string ipAddress, CancellationToken cancellationToken = default);
    }
}