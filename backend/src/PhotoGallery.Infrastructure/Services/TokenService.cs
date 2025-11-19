using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PhotoGallery.Application.Interfaces.Services;
using PhotoGallery.Domain.Entities;
using PhotoGallery.Infrastructure.Data;
using PhotoGallery.ServiceDefaults.Settings;

namespace PhotoGallery.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly SymmetricSecurityKey _signingKey;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        public TokenService(ApplicationDbContext context, IOptions<JwtSettings> jwtSettings)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            _tokenHandler = new JwtSecurityTokenHandler();
        }

        public string GenerateAccessToken(User user, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(JwtRegisteredClaimNames.UniqueName, user.UserName!),
                new(ClaimTypes.Name, user.UserName!),
                new(JwtRegisteredClaimNames.Email, user.Email!),
                new(ClaimTypes.Email, user.Email!),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var creds = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationInMinutes),
                signingCredentials: creds
            );

            return _tokenHandler.WriteToken(token);
        }

        public async Task<RefreshToken> GenerateRefreshTokenAsync(
            Guid userId,
            string ipAddress,
            string? replacedToken = null,
            CancellationToken cancellationToken = default)
        {
            var refreshToken = new RefreshToken
            {
                UserId = userId,
                Token = GenerateSecureRandomToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationInDays),
                CreatedByIp = ipAddress,
                ReplacedByToken = replacedToken
            };

            await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await RemoveOldRefreshTokensAsync(userId, cancellationToken);

            return refreshToken;
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
        }

        public async Task RevokeTokenAsync(string token, string ipAddress, string reason = "Revoked by user", CancellationToken cancellationToken = default)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

            if (refreshToken == null || !refreshToken.IsActive)
                return;

            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReasonRevoked = reason;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> RevokeAllUserTokensAsync(Guid userId, string ipAddress, CancellationToken cancellationToken = default)
        {
            var affectedRows = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(rt => rt.RevokedAt, DateTime.UtcNow)
                        .SetProperty(rt => rt.RevokedByIp, ipAddress)
                        .SetProperty(rt => rt.ReasonRevoked, "Revoked all tokens by user"),
                    cancellationToken
                );

            return affectedRows;
        }

        private async Task RemoveOldRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var threshold = DateTime.UtcNow.AddDays(-_jwtSettings.RefreshTokenExpirationInDays * 2);

                await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId &&
                                rt.RevokedAt != null &&
                                rt.CreatedAt < threshold)
                    .ExecuteDeleteAsync(cancellationToken);
            }
            catch
            {
                // Silent fail
            }
        }

        private static string GenerateSecureRandomToken()
        {
            Span<byte> randomBytes = stackalloc byte[64];
            RandomNumberGenerator.Fill(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
