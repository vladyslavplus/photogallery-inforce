namespace PhotoGallery.Application.Interfaces.Services
{
    public interface IFileStorageService
    {
        string GetPhotoUrl(string filePath);
        string GetThumbnailUrl(string thumbnailPath);
        Task<(string filePath, string thumbnailPath)> SavePhotoAsync(Stream photoStream, string fileName, Guid albumId);
        Task DeletePhotoAsync(string filePath, string thumbnailPath);
    }
}
