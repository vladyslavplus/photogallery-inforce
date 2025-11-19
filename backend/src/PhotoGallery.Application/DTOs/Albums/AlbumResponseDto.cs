namespace PhotoGallery.Application.DTOs.Albums
{
    public class AlbumResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int PhotosCount { get; set; }
        public string? CoverPhotoUrl { get; set; }
    }
}
