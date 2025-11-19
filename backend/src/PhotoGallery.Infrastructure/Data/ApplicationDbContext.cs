using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PhotoGallery.Domain.Entities;

namespace PhotoGallery.Infrastructure.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
    {
        public DbSet<Album> Albums => Set<Album>();
        public DbSet<Photo> Photos => Set<Photo>();
        public DbSet<PhotoLike> PhotoLikes => Set<PhotoLike>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Album>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(a => a.Description)
                    .HasMaxLength(1000);

                entity.HasOne(a => a.User)
                    .WithMany(u => u.Albums)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(a => a.UserId);
                entity.HasIndex(a => a.CreatedAt);
            });

            builder.Entity<Photo>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.FileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(p => p.FilePath)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(p => p.ThumbnailPath)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(p => p.Title)
                    .HasMaxLength(200);

                entity.HasOne(p => p.Album)
                    .WithMany(a => a.Photos)
                    .HasForeignKey(p => p.AlbumId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(p => p.AlbumId);
                entity.HasIndex(p => p.UploadedAt);
            });

            builder.Entity<PhotoLike>(entity =>
            {
                entity.HasKey(pl => pl.Id);

                entity.HasOne(pl => pl.Photo)
                    .WithMany(p => p.Likes)
                    .HasForeignKey(pl => pl.PhotoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pl => pl.User)
                    .WithMany(u => u.PhotoLikes)
                    .HasForeignKey(pl => pl.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(pl => new { pl.PhotoId, pl.UserId })
                    .IsUnique();
            });

            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(rt => rt.Id);

                entity.Property(rt => rt.Token)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(rt => rt.CreatedByIp)
                    .HasMaxLength(50);

                entity.Property(rt => rt.RevokedByIp)
                    .HasMaxLength(50);

                entity.Property(rt => rt.ReplacedByToken)
                    .HasMaxLength(500);

                entity.Property(rt => rt.ReasonRevoked)
                    .HasMaxLength(200);

                entity.HasOne(rt => rt.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(rt => rt.Token);
                entity.HasIndex(rt => rt.UserId);
                entity.HasIndex(rt => rt.ExpiresAt);
            });
        }
    }
}