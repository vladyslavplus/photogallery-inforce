using Microsoft.EntityFrameworkCore;
using PhotoGallery.Application.Common;
using PhotoGallery.Application.Interfaces.Repositories;
using PhotoGallery.Domain.Entities;
using PhotoGallery.Infrastructure.Data;

namespace PhotoGallery.Infrastructure.Repositories
{
    public class AlbumRepository : IAlbumRepository
    {
        private readonly ApplicationDbContext _context;

        public AlbumRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<Album>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.Albums
                .Include(a => a.User)
                .AsNoTracking();

            var totalCount = await query.CountAsync(cancellationToken);

            var albums = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var albumIds = albums.Select(a => a.Id).ToList();
            var coverPhotos = await _context.Photos
                .Where(p => albumIds.Contains(p.AlbumId))
                .GroupBy(p => p.AlbumId)
                .Select(g => g.OrderBy(p => p.UploadedAt).First())
                .ToListAsync(cancellationToken);

            var photoCounts = await _context.Photos
                .Where(p => albumIds.Contains(p.AlbumId))
                .GroupBy(p => p.AlbumId)
                .Select(g => new { AlbumId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            foreach (var album in albums)
            {
                album.Photos = coverPhotos.Where(p => p.AlbumId == album.Id).ToList();
            }

            return new PagedResult<Album>
            {
                Items = albums,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                AdditionalData = photoCounts.ToDictionary(pc => pc.AlbumId, pc => pc.Count)
            };
        }

        public async Task<PagedResult<Album>> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.Albums
                .Include(a => a.User)
                .Where(a => a.UserId == userId)
                .AsNoTracking();

            var totalCount = await query.CountAsync(cancellationToken);

            var albums = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var albumIds = albums.Select(a => a.Id).ToList();
            var coverPhotos = await _context.Photos
                .Where(p => albumIds.Contains(p.AlbumId))
                .GroupBy(p => p.AlbumId)
                .Select(g => g.OrderBy(p => p.UploadedAt).First())
                .ToListAsync(cancellationToken);

            var photoCounts = await _context.Photos
                .Where(p => albumIds.Contains(p.AlbumId))
                .GroupBy(p => p.AlbumId)
                .Select(g => new { AlbumId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            foreach (var album in albums)
            {
                album.Photos = coverPhotos.Where(p => p.AlbumId == album.Id).ToList();
            }

            return new PagedResult<Album>
            {
                Items = albums,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                AdditionalData = photoCounts.ToDictionary(pc => pc.AlbumId, pc => pc.Count)
            };
        }

        public async Task<Album?> GetByIdAsync(
            Guid id,
            bool includePhotos = false,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Albums.AsQueryable();

            query = query.Include(a => a.User);

            if (includePhotos)
            {
                query = query.Include(a => a.Photos);
            }
            else
            {
                query = query.Include(a => a.Photos
                    .OrderBy(p => p.UploadedAt)
                    .Take(1));
            }

            return await query.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<Album> CreateAsync(Album album, CancellationToken cancellationToken = default)
        {
            await _context.Albums.AddAsync(album, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await _context.Entry(album)
                .Reference(a => a.User)
                .LoadAsync(cancellationToken);

            return album;
        }

        public async Task UpdateAsync(Album album, CancellationToken cancellationToken = default)
        {
            _context.Albums.Update(album);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Album album, CancellationToken cancellationToken = default)
        {
            _context.Albums.Remove(album);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
