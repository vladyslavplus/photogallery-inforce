using Microsoft.AspNetCore.Identity;

namespace PhotoGallery.Domain.Entities
{
    public class User : IdentityUser<Guid>
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Album> Albums { get; set; } = new List<Album>();
        public ICollection<PhotoLike> PhotoLikes { get; set; } = new List<PhotoLike>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}