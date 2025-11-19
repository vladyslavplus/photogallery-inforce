using PhotoGallery.Application.Common;
using PhotoGallery.Application.DTOs.Albums;

namespace PhotoGallery.Application.Interfaces.Services
{
    public interface IAlbumService
    {
        Task<PagedResult<AlbumResponseDto>> GetAllAlbumsAsync(int pageNumber = 1, int pageSize = 5, CancellationToken cancellationToken = default);
        Task<PagedResult<AlbumResponseDto>> GetUserAlbumsAsync(Guid userId, int pageNumber = 1, int pageSize = 5, CancellationToken cancellationToken = default);
        Task<AlbumResponseDto?> GetAlbumByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<AlbumResponseDto> CreateAlbumAsync(AlbumCreateDto dto, Guid userId, CancellationToken cancellationToken = default);
        Task<AlbumResponseDto?> UpdateAlbumAsync(Guid id, AlbumUpdateDto dto, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> DeleteAlbumAsync(Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken = default);
    }
}
