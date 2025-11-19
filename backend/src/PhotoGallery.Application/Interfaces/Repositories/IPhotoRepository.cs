using PhotoGallery.Application.Common;
using PhotoGallery.Domain.Entities;

namespace PhotoGallery.Application.Interfaces.Repositories
{
    public interface IPhotoRepository
    {
        Task<PagedResult<Photo>> GetByAlbumIdAsync(Guid albumId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<Photo?> GetByIdAsync(Guid id, bool includeLikes = false, CancellationToken cancellationToken = default);
        Task<Photo> CreateAsync(Photo photo, CancellationToken cancellationToken = default);
        Task DeleteAsync(Photo photo, CancellationToken cancellationToken = default);
        Task<PhotoLike?> GetLikeAsync(Guid photoId, Guid userId, CancellationToken cancellationToken = default);
        Task AddLikeAsync(PhotoLike like, CancellationToken cancellationToken = default);
        Task RemoveLikeAsync(PhotoLike like, CancellationToken cancellationToken = default);
        Task UpdateLikeAsync(PhotoLike like, CancellationToken cancellationToken = default);
    }
}
