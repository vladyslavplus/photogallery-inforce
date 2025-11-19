using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using PhotoGallery.Application.DTOs.Auth;
using PhotoGallery.Application.DTOs.Photos;
using PhotoGallery.Domain.Entities;

namespace PhotoGallery.Tests.Helpers
{
    public static class TestDataFactory
    {
        public static User CreateUser(string userName, Guid? userId = null)
        {
            return new User
            {
                Id = userId ?? Guid.NewGuid(),
                UserName = userName,
                Email = $"{userName}@test.com",
                CreatedAt = DateTime.UtcNow
            };
        }

        public static Album CreateAlbum(string title, User user, Guid? albumId = null)
        {
            return new Album
            {
                Id = albumId ?? Guid.NewGuid(),
                Title = title,
                UserId = user.Id,
                User = user,
                Photos = new List<Photo>(),
                CreatedAt = DateTime.UtcNow
            };
        }

        public static Photo CreatePhoto(string fileName, Guid albumId, Guid? photoId = null, Album? album = null)
        {
            return new Photo
            {
                Id = photoId ?? Guid.NewGuid(),
                FileName = fileName,
                FilePath = $"/photos/{fileName}",
                ThumbnailPath = $"/thumbnails/{fileName}",
                AlbumId = albumId,
                Album = album!,
                UploadedAt = DateTime.UtcNow,
                Likes = new List<PhotoLike>()
            };
        }

        public static PhotoLike CreatePhotoLike(Guid photoId, Guid userId, bool isLike = true)
        {
            return new PhotoLike
            {
                Id = Guid.NewGuid(),
                PhotoId = photoId,
                UserId = userId,
                IsLike = isLike,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static PhotoUploadDto CreatePhotoUploadDto(Guid albumId, string fileName = "test.jpg", string? title = null)
        {
            var fileMock = new Mock<IFormFile>();
            var content = "fake file content"u8.ToArray();
            var ms = new MemoryStream(content);

            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(content.Length);
            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.ContentType).Returns("image/jpeg");

            return new PhotoUploadDto
            {
                File = fileMock.Object,
                AlbumId = albumId,
                Title = title
            };
        }

        public static RegisterDto CreateRegisterDto(string userName = "testuser", string email = "test@test.com", string password = "Password123")
        {
            return new RegisterDto
            {
                UserName = userName,
                Email = email,
                Password = password
            };
        }

        public static LoginDto CreateLoginDto(string email = "test@test.com", string password = "Password123")
        {
            return new LoginDto
            {
                Email = email,
                Password = password
            };
        }

        public static RefreshToken CreateRefreshToken(Guid userId, string token = "test-refresh-token")
        {
            return new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = "127.0.0.1"
            };
        }

        public static IdentityResult CreateIdentityResult(bool succeeded, params string[] errors)
        {
            if (succeeded)
            {
                return IdentityResult.Success;
            }

            var identityErrors = errors.Select(e => new IdentityError { Description = e }).ToArray();
            return IdentityResult.Failed(identityErrors);
        }
    }
}