namespace PhotoGallery.Application.DTOs.Photos
{
    public class PhotoResponseDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string? Title { get; set; }
        public DateTime UploadedAt { get; set; }
        public Guid AlbumId { get; set; }
        public string AlbumTitle { get; set; } = string.Empty;
        public int LikesCount { get; set; }
        public int DislikesCount { get; set; }
        public bool? CurrentUserLiked { get; set; }
    }
}
