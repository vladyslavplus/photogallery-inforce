using Microsoft.AspNetCore.Http;

namespace PhotoGallery.Application.DTOs.Photos
{
    public class PhotoUploadDto
    {
        public IFormFile? File { get; set; }
        public string? Title { get; set; }
        public Guid AlbumId { get; set; }
    }
}
