using Microsoft.EntityFrameworkCore;
using PhotoGallery.Application.Common;
using PhotoGallery.Application.Interfaces.Repositories;
using PhotoGallery.Domain.Entities;
using PhotoGallery.Infrastructure.Data;

namespace PhotoGallery.Infrastructure.Repositories
{
    public class PhotoRepository : IPhotoRepository
    {
        private readonly ApplicationDbContext _context;

        public PhotoRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<Photo>> GetByAlbumIdAsync(Guid albumId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.Photos
                .Include(p => p.Album)
                .Include(p => p.Likes)
                .Where(p => p.AlbumId == albumId)
                .AsNoTracking();

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(p => p.UploadedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Photo>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<Photo?> GetByIdAsync(Guid id, bool includeLikes = false, CancellationToken cancellationToken = default)
        {
            var query = _context.Photos.AsQueryable();

            query = query.Include(p => p.Album);

            if (includeLikes)
            {
                query = query.Include(p => p.Likes);
            }

            return await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<Photo> CreateAsync(Photo photo, CancellationToken cancellationToken = default)
        {
            await _context.Photos.AddAsync(photo, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await _context.Entry(photo).Reference(p => p.Album).LoadAsync(cancellationToken);
            return photo;
        }

        public async Task DeleteAsync(Photo photo, CancellationToken cancellationToken = default)
        {
            _context.Photos.Remove(photo);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<PhotoLike?> GetLikeAsync(Guid photoId, Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.PhotoLikes.FirstOrDefaultAsync(pl => pl.PhotoId == photoId && pl.UserId == userId, cancellationToken);
        }

        public async Task AddLikeAsync(PhotoLike like, CancellationToken cancellationToken = default)
        {
            await _context.PhotoLikes.AddAsync(like, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveLikeAsync(PhotoLike like, CancellationToken cancellationToken = default)
        {
            _context.PhotoLikes.Remove(like);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateLikeAsync(PhotoLike like, CancellationToken cancellationToken = default)
        {
            _context.PhotoLikes.Update(like);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
