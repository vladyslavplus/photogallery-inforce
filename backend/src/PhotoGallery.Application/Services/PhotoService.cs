using PhotoGallery.Application.Common;
using PhotoGallery.Application.DTOs.Photos;
using PhotoGallery.Application.Interfaces.Repositories;
using PhotoGallery.Application.Interfaces.Services;
using PhotoGallery.Domain.Entities;

namespace PhotoGallery.Application.Services
{
    public class PhotoService : IPhotoService
    {
        private readonly IPhotoRepository _photoRepository;
        private readonly IAlbumRepository _albumRepository;
        private readonly IFileStorageService _fileStorage;

        public PhotoService(IPhotoRepository photoRepository, IAlbumRepository albumRepository, IFileStorageService fileStorage)
        {
            _photoRepository = photoRepository;
            _albumRepository = albumRepository;
            _fileStorage = fileStorage;
        }

        public async Task<PagedResult<PhotoResponseDto>> GetAlbumPhotosAsync(Guid albumId, int pageNumber = 1, int pageSize = 5, Guid? currentUserId = null, CancellationToken cancellationToken = default)
        {
            var result = await _photoRepository.GetByAlbumIdAsync(albumId, pageNumber, pageSize, cancellationToken);

            return new PagedResult<PhotoResponseDto>
            {
                Items = result.Items.Select(p => MapToResponseDto(p, currentUserId)).ToList(),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
        }

        public async Task<PhotoResponseDto?> GetPhotoByIdAsync(Guid id, Guid? currentUserId = null, CancellationToken cancellationToken = default)
        {
            var photo = await _photoRepository.GetByIdAsync(id, true, cancellationToken);
            return photo == null ? null : MapToResponseDto(photo, currentUserId);
        }

        public async Task<PhotoResponseDto> UploadPhotoAsync(PhotoUploadDto dto, Guid userId, CancellationToken cancellationToken = default)
        {
            var album = await _albumRepository.GetByIdAsync(dto.AlbumId, false, cancellationToken);

            if (album == null)
                throw new KeyNotFoundException("Album not found");

            if (album.UserId != userId)
                throw new UnauthorizedAccessException("You don't have permission to upload photos to this album");

            using var stream = dto.File!.OpenReadStream();
            var (filePath, thumbnailPath) = await _fileStorage.SavePhotoAsync(stream, dto.File.FileName, dto.AlbumId);

            var photo = new Photo
            {
                FileName = dto.File.FileName,
                FilePath = filePath,
                ThumbnailPath = thumbnailPath,
                Title = dto.Title,
                AlbumId = dto.AlbumId
            };

            var createdPhoto = await _photoRepository.CreateAsync(photo, cancellationToken);
            return MapToResponseDto(createdPhoto, userId);
        }

        public async Task<bool> DeletePhotoAsync(Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken = default)
        {
            var photo = await _photoRepository.GetByIdAsync(id, false, cancellationToken);

            if (photo == null)
                return false;

            if (!isAdmin && photo.Album.UserId != userId)
                throw new UnauthorizedAccessException("You don't have permission to delete this photo");

            await _fileStorage.DeletePhotoAsync(photo.FilePath, photo.ThumbnailPath);
            await _photoRepository.DeleteAsync(photo, cancellationToken);

            return true;
        }

        public async Task<PhotoResponseDto> ToggleLikeAsync(Guid photoId, Guid userId, bool isLike, CancellationToken cancellationToken = default)
        {
            var photo = await _photoRepository.GetByIdAsync(photoId, true, cancellationToken);

            if (photo == null)
                throw new KeyNotFoundException("Photo not found");

            var existingLike = await _photoRepository.GetLikeAsync(photoId, userId, cancellationToken);

            if (existingLike == null)
            {
                var newLike = new PhotoLike
                {
                    PhotoId = photoId,
                    UserId = userId,
                    IsLike = isLike
                };
                await _photoRepository.AddLikeAsync(newLike, cancellationToken);
            }
            else if (existingLike.IsLike == isLike)
            {
                await _photoRepository.RemoveLikeAsync(existingLike, cancellationToken);
            }
            else
            {
                existingLike.IsLike = isLike;
                await _photoRepository.UpdateLikeAsync(existingLike, cancellationToken);
            }

            var updatedPhoto = await _photoRepository.GetByIdAsync(photoId, true, cancellationToken);
            return MapToResponseDto(updatedPhoto!, userId);
        }

        private PhotoResponseDto MapToResponseDto(Photo photo, Guid? currentUserId)
        {
            var userLike = currentUserId.HasValue ? photo.Likes.FirstOrDefault(l => l.UserId == currentUserId.Value) : null;

            return new PhotoResponseDto
            {
                Id = photo.Id,
                FileName = photo.FileName,
                PhotoUrl = _fileStorage.GetPhotoUrl(photo.FilePath),
                ThumbnailUrl = _fileStorage.GetThumbnailUrl(photo.ThumbnailPath),
                Title = photo.Title,
                UploadedAt = photo.UploadedAt,
                AlbumId = photo.AlbumId,
                AlbumTitle = photo.Album?.Title ?? "Unknown",
                LikesCount = photo.Likes.Count(l => l.IsLike),
                DislikesCount = photo.Likes.Count(l => !l.IsLike),
                CurrentUserLiked = userLike?.IsLike
            };
        }
    }
}
