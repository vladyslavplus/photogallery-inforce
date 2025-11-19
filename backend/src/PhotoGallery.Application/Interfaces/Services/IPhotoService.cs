using PhotoGallery.Application.Common;
using PhotoGallery.Application.DTOs.Photos;

namespace PhotoGallery.Application.Interfaces.Services
{
    public interface IPhotoService
    {
        Task<PagedResult<PhotoResponseDto>> GetAlbumPhotosAsync(Guid albumId, int pageNumber = 1, int pageSize = 5, Guid? currentUserId = null, CancellationToken cancellationToken = default);
        Task<PhotoResponseDto?> GetPhotoByIdAsync(Guid id, Guid? currentUserId = null, CancellationToken cancellationToken = default);
        Task<PhotoResponseDto> UploadPhotoAsync(PhotoUploadDto dto, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> DeletePhotoAsync(Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
        Task<PhotoResponseDto> ToggleLikeAsync(Guid photoId, Guid userId, bool isLike, CancellationToken cancellationToken = default);
    }
}
