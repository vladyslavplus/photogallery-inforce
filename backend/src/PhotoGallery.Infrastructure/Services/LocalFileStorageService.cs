using PhotoGallery.Application.Interfaces.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace PhotoGallery.Infrastructure.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IPathProvider _pathProvider;
        private const string PhotosFolder = "uploads/photos";
        private const string ThumbnailsFolder = "uploads/thumbnails";
        private const int ThumbnailWidth = 300;
        private const int ThumbnailHeight = 300;

        public LocalFileStorageService(IPathProvider pathProvider)
        {
            _pathProvider = pathProvider;
            EnsureDirectoriesExist();
        }

        public async Task<(string filePath, string thumbnailPath)> SavePhotoAsync(
            Stream photoStream,
            string fileName,
            Guid albumId)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var uniqueFileName = $"{albumId}_{Guid.NewGuid()}{extension}";

            var photoRelativePath = Path.Combine(PhotosFolder, uniqueFileName);
            var thumbnailRelativePath = Path.Combine(ThumbnailsFolder, uniqueFileName);

            var fullPhotoPath = Path.Combine(_pathProvider.WebRootPath, photoRelativePath);
            var fullThumbnailPath = Path.Combine(_pathProvider.WebRootPath, thumbnailRelativePath);

            using (var image = await Image.LoadAsync(photoStream))
            {
                await image.SaveAsync(fullPhotoPath);

                var thumbnail = image.Clone(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(ThumbnailWidth, ThumbnailHeight),
                    Mode = ResizeMode.Crop
                }));

                await thumbnail.SaveAsync(fullThumbnailPath);
            }

            return (photoRelativePath, thumbnailRelativePath);
        }

        public Task DeletePhotoAsync(string filePath, string thumbnailPath)
        {
            var fullPhotoPath = Path.Combine(_pathProvider.WebRootPath, filePath);
            var fullThumbnailPath = Path.Combine(_pathProvider.WebRootPath, thumbnailPath);

            if (File.Exists(fullPhotoPath)) File.Delete(fullPhotoPath);
            if (File.Exists(fullThumbnailPath)) File.Delete(fullThumbnailPath);

            return Task.CompletedTask;
        }

        public string GetPhotoUrl(string filePath) =>
            "/" + filePath.Replace("\\", "/");

        public string GetThumbnailUrl(string thumbnailPath) =>
            "/" + thumbnailPath.Replace("\\", "/");

        private void EnsureDirectoriesExist()
        {
            var photosDir = Path.Combine(_pathProvider.WebRootPath, PhotosFolder);
            var thumbnailsDir = Path.Combine(_pathProvider.WebRootPath, ThumbnailsFolder);

            Directory.CreateDirectory(photosDir);
            Directory.CreateDirectory(thumbnailsDir);
        }
    }
}
