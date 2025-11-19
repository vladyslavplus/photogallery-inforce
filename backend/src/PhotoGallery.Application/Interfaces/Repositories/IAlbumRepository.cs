using PhotoGallery.Application.Common;
using PhotoGallery.Domain.Entities;

namespace PhotoGallery.Application.Interfaces.Repositories
{
    public interface IAlbumRepository
    {
        Task<PagedResult<Album>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<PagedResult<Album>> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<Album?> GetByIdAsync(Guid id, bool includePhotos = false, CancellationToken cancellationToken = default);
        Task<Album> CreateAsync(Album album, CancellationToken cancellationToken = default);
        Task UpdateAsync( Album album, CancellationToken cancellationToken = default);
        Task DeleteAsync( Album album, CancellationToken cancellationToken = default);
    }
}
