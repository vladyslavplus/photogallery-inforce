using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhotoGallery.Application.Interfaces.Services;
using PhotoGallery.Domain.Entities;

namespace PhotoGallery.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                var userManager = services.GetRequiredService<UserManager<User>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                var fileStorage = services.GetRequiredService<IFileStorageService>();

                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully");

                await SeedRolesAsync(roleManager, logger);
                await SeedUsersAsync(userManager, logger);
                await SeedAlbumsAndPhotosAsync(context, userManager, fileStorage, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database");
            }
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager, ILogger logger)
        {
            string[] roles = { "Admin", "User" };

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    logger.LogInformation("Creating role: {RoleName}", roleName);
                    await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                }
            }
        }

        private static async Task SeedUsersAsync(UserManager<User> userManager, ILogger logger)
        {
            var adminEmail = "admin@admin.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new User
                {
                    UserName = "admin",
                    Email = adminEmail,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(admin, "Admin@1234");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                    logger.LogInformation("Admin user created: {Email}", adminEmail);
                }
            }

            var userEmail = "user@user.com";
            if (await userManager.FindByEmailAsync(userEmail) == null)
            {
                var user = new User
                {
                    UserName = "testuser",
                    Email = userEmail,
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(user, "User@1234");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "User");
                    logger.LogInformation("Test user created: {Email}", userEmail);
                }
            }
        }

        private static async Task SeedAlbumsAndPhotosAsync(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IFileStorageService fileStorage,
            ILogger logger)
        {
            if (!await context.Albums.AnyAsync())
            {
                var testUser = await userManager.FindByEmailAsync("user@user.com");
                var admin = await userManager.FindByEmailAsync("admin@admin.com");

                if (testUser == null || admin == null)
                {
                    logger.LogWarning("Cannot seed albums: users not found");
                    return;
                }

                var albums = new List<Album>
                {
                    new()
                    {
                        Title = "Nature Photography",
                        Description = "Beautiful landscapes and wildlife",
                        UserId = testUser.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-30)
                    },
                    new()
                    {
                        Title = "City Life",
                        Description = "Urban photography collection",
                        UserId = testUser.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-25)
                    },
                    new()
                    {
                        Title = "Admin Gallery",
                        Description = "Official photo collection",
                        UserId = admin.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-20)
                    },
                    new()
                    {
                        Title = "Travel Moments",
                        Description = "Adventures around the world",
                        UserId = testUser.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-15)
                    },
                    new()
                    {
                        Title = "Portrait Series",
                        Description = "Professional portrait photography",
                        UserId = admin.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-12)
                    },
                    new()
                    {
                        Title = "Food Photography",
                        Description = "Delicious culinary moments",
                        UserId = testUser.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-8)
                    },
                    new()
                    {
                        Title = "Architecture & Design",
                        Description = "Modern buildings and structures",
                        UserId = admin.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    },
                    new()
                    {
                        Title = "Events & Celebrations",
                        Description = "Special moments captured",
                        UserId = testUser.Id,
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    }
                };

                await context.Albums.AddRangeAsync(albums);
                await context.SaveChangesAsync();

                logger.LogInformation("Seeded {Count} albums", albums.Count);
            }
            else
            {
                logger.LogInformation("Albums already exist, skipping seeding albums");
            }

            if (!await context.Photos.AnyAsync())
            {
                var albums = await context.Albums.ToListAsync();
                await SeedPhotosAsync(context, albums, fileStorage, logger);
            }
            else
            {
                logger.LogInformation("Photos already exist, skipping seeding photos");
            }
        }

        private static async Task SeedPhotosAsync(
            ApplicationDbContext context,
            List<Album> albums,
            IFileStorageService fileStorage,
            ILogger logger)
        {
            var random = new Random();
            using var httpClient = new HttpClient();

            var photoTitles = new[]
            {
                "Sunset", "Mountain View", "Ocean Waves", "City Lights", "Forest Path",
                "Wildlife", "Architecture", "Abstract", "Portrait", "Landscape"
            };

            var allUserIds = albums.Select(a => a.UserId).Distinct().ToList();

            foreach (var album in albums)
            {
                if (await context.Photos.AnyAsync(p => p.AlbumId == album.Id))
                {
                    logger.LogInformation("Album {AlbumId} already has photos, skipping", album.Id);
                    continue;
                }

                var photoCount = random.Next(3, 7);
                var photosToAdd = new List<Photo>();
                var likesToAdd = new List<PhotoLike>();

                for (int i = 0; i < photoCount; i++)
                {
                    try
                    {
                        var imageUrl = $"https://picsum.photos/600/400?random={Guid.NewGuid()}";
                        var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);

                        using var stream = new MemoryStream(imageBytes);
                        var fileName = $"photo_{i + 1}.jpg";

                        var (filePath, thumbnailPath) = await fileStorage.SavePhotoAsync(stream, fileName, album.Id);

                        var photo = new Photo
                        {
                            FileName = fileName,
                            FilePath = filePath,
                            ThumbnailPath = thumbnailPath,
                            Title = photoTitles[random.Next(photoTitles.Length)],
                            AlbumId = album.Id,
                            UploadedAt = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                        };

                        photosToAdd.Add(photo);

                        if (random.Next(100) > 50)
                        {
                            var likeCount = random.Next(1, Math.Min(allUserIds.Count, 3));
                            likesToAdd.AddRange(
                                allUserIds
                                    .OrderBy(_ => random.Next())
                                    .Take(likeCount)
                                    .Select(userId => new PhotoLike
                                    {
                                        Photo = photo,
                                        UserId = userId,
                                        IsLike = random.Next(100) > 30,
                                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 20))
                                    })
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to seed photo {Index} for album {AlbumId}", i, album.Id);
                    }
                }

                if (photosToAdd.Count > 0)
                {
                    await context.Photos.AddRangeAsync(photosToAdd);
                    await context.PhotoLikes.AddRangeAsync(likesToAdd);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded {Count} photos for album {AlbumId}", photosToAdd.Count, album.Id);
                }
            }
        }
    }
}
