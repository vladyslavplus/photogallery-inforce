namespace PhotoGallery.Domain.Entities
{
    public class Photo
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ThumbnailPath { get; set; } = string.Empty;
        public string? Title { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public Guid AlbumId { get; set; }
        public Album Album { get; set; } = null!;
        public ICollection<PhotoLike> Likes { get; set; } = new List<PhotoLike>();
        public int LikesCount { get; set; }
        public int DislikesCount { get; set; }
    }
}