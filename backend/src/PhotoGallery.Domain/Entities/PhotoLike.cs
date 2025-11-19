namespace PhotoGallery.Domain.Entities
{

    public class PhotoLike
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public bool IsLike { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid PhotoId { get; set; }
        public Guid UserId { get; set; }
        public Photo Photo { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
