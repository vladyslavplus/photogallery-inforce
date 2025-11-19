using PhotoGallery.Application.Common;
using PhotoGallery.Application.DTOs.Albums;
using PhotoGallery.Application.Interfaces.Repositories;
using PhotoGallery.Application.Interfaces.Services;
using PhotoGallery.Domain.Entities;

namespace PhotoGallery.Application.Services
{
    public class AlbumService : IAlbumService
    {
        private readonly IAlbumRepository _albumRepository;
        private readonly IFileStorageService _fileStorage;

        public AlbumService(IAlbumRepository albumRepository, IFileStorageService fileStorage)
        {
            _albumRepository = albumRepository;
            _fileStorage = fileStorage;
        }

        public async Task<PagedResult<AlbumResponseDto>> GetAllAlbumsAsync(int pageNumber = 1, int pageSize = 5, CancellationToken cancellationToken = default)
        {
            var result = await _albumRepository.GetAllAsync(pageNumber, pageSize, cancellationToken);

            return new PagedResult<AlbumResponseDto>
            {
                Items = result.Items.Select(album => MapToResponseDto(album, result.AdditionalData)).ToList(),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
        }

        public async Task<PagedResult<AlbumResponseDto>> GetUserAlbumsAsync(Guid userId, int pageNumber = 1, int pageSize = 5, CancellationToken cancellationToken = default)
        {
            var result = await _albumRepository.GetByUserIdAsync(userId, pageNumber, pageSize, cancellationToken);

            return new PagedResult<AlbumResponseDto>
            {
                Items = result.Items.Select(album => MapToResponseDto(album, result.AdditionalData)).ToList(),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
        }

        public async Task<AlbumResponseDto?> GetAlbumByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var album = await _albumRepository.GetByIdAsync(id, false, cancellationToken);
            return album == null ? null : MapToResponseDto(album);
        }

        public async Task<AlbumResponseDto> CreateAlbumAsync(AlbumCreateDto dto, Guid userId, CancellationToken cancellationToken = default)
        {
            var album = new Album
            {
                Title = dto.Title,
                Description = dto.Description,
                UserId = userId
            };

            var createdAlbum = await _albumRepository.CreateAsync(album, cancellationToken);
            return MapToResponseDto(createdAlbum);
        }

        public async Task<AlbumResponseDto?> UpdateAlbumAsync(Guid id, AlbumUpdateDto dto, Guid userId, CancellationToken cancellationToken = default)
        {
            var album = await _albumRepository.GetByIdAsync(id, false, cancellationToken);

            if (album == null)
                return null;

            if (album.UserId != userId)
                throw new UnauthorizedAccessException("You don't have permission to update this album");

            if (dto.Title != null)
                album.Title = dto.Title;

            if (dto.Description != null)
                album.Description = dto.Description;

            await _albumRepository.UpdateAsync(album, cancellationToken);

            return MapToResponseDto(album);
        }

        public async Task<bool> DeleteAlbumAsync(Guid id, Guid userId, bool isAdmin, CancellationToken cancellationToken = default)
        {
            var album = await _albumRepository.GetByIdAsync(id, true, cancellationToken);

            if (album == null)
                return false;

            if (!isAdmin && album.UserId != userId)
                throw new UnauthorizedAccessException("You don't have permission to delete this album");

            foreach (var photo in album.Photos)
            {
                await _fileStorage.DeletePhotoAsync(photo.FilePath, photo.ThumbnailPath);
            }

            await _albumRepository.DeleteAsync(album, cancellationToken);

            return true;
        }

        private AlbumResponseDto MapToResponseDto(Album album, Dictionary<Guid, int>? photoCounts = null)
        {
            var coverPhoto = album.Photos.FirstOrDefault();
            var photoCount = photoCounts?.GetValueOrDefault(album.Id) ?? album.Photos.Count;

            return new AlbumResponseDto
            {
                Id = album.Id,
                Title = album.Title,
                Description = album.Description,
                CreatedAt = album.CreatedAt,
                UserId = album.UserId,
                UserName = album.User?.UserName ?? "Unknown",
                PhotosCount = photoCount,
                CoverPhotoUrl = coverPhoto != null ? _fileStorage.GetThumbnailUrl(coverPhoto.ThumbnailPath) : null
            };
        }
    }
}
