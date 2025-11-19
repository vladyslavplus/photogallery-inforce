using FluentAssertions;
using Moq;
using PhotoGallery.Application.Common;
using PhotoGallery.Application.Interfaces.Repositories;
using PhotoGallery.Application.Interfaces.Services;
using PhotoGallery.Application.Services;
using PhotoGallery.Domain.Entities;
using PhotoGallery.Tests.Helpers;

namespace PhotoGallery.Tests.Services
{
    public class PhotoServiceTests
    {
        private readonly Mock<IPhotoRepository> _photoRepositoryMock;
        private readonly Mock<IAlbumRepository> _albumRepositoryMock;
        private readonly Mock<IFileStorageService> _fileStorageMock;
        private readonly PhotoService _sut;

        public PhotoServiceTests()
        {
            _photoRepositoryMock = new Mock<IPhotoRepository>();
            _albumRepositoryMock = new Mock<IAlbumRepository>();
            _fileStorageMock = new Mock<IFileStorageService>();
            _sut = new PhotoService(_photoRepositoryMock.Object, _albumRepositoryMock.Object, _fileStorageMock.Object);
        }

        #region GetAlbumPhotosAsync

        [Fact]
        public async Task GetAlbumPhotosAsync_ShouldReturnPagedPhotos_WhenPhotosExist()
        {
            // Arrange
            var albumId = Guid.NewGuid();
            var user = TestDataFactory.CreateUser("testuser");
            var album = TestDataFactory.CreateAlbum("Test Album", user, albumId);

            var photos = new List<Photo>
            {
                TestDataFactory.CreatePhoto("photo1.jpg", albumId, album: album),
                TestDataFactory.CreatePhoto("photo2.jpg", albumId, album: album)
            };

            var pagedResult = new PagedResult<Photo>
            {
                Items = photos,
                TotalCount = 2,
                PageNumber = 1,
                PageSize = 5
            };

            _photoRepositoryMock.Setup(x => x.GetByAlbumIdAsync(albumId, 1, 5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(pagedResult);

            _fileStorageMock.Setup(x => x.GetPhotoUrl(It.IsAny<string>()))
                .Returns((string path) => $"http://localhost{path}");
            _fileStorageMock.Setup(x => x.GetThumbnailUrl(It.IsAny<string>()))
                .Returns((string path) => $"http://localhost{path}");

            // Act
            var result = await _sut.GetAlbumPhotosAsync(albumId, 1, 5);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            result.Items.Should().OnlyContain(p => p.AlbumId == albumId);
            _photoRepositoryMock.Verify(x => x.GetByAlbumIdAsync(albumId, 1, 5, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region GetPhotoByIdAsync

        [Fact]
        public async Task GetPhotoByIdAsync_ShouldReturnPhoto_WhenPhotoExists()
        {
            // Arrange
            var photoId = Guid.NewGuid();
            var user = TestDataFactory.CreateUser("testuser");
            var album = TestDataFactory.CreateAlbum("Test Album", user);
            var photo = TestDataFactory.CreatePhoto("test.jpg", album.Id, photoId, album);

            _photoRepositoryMock.Setup(x => x.GetByIdAsync(photoId, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(photo);

            _fileStorageMock.Setup(x => x.GetPhotoUrl(It.IsAny<string>()))
                .Returns((string path) => $"http://localhost{path}");
            _fileStorageMock.Setup(x => x.GetThumbnailUrl(It.IsAny<string>()))
                .Returns((string path) => $"http://localhost{path}");

            // Act
            var result = await _sut.GetPhotoByIdAsync(photoId, user.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(photoId);
            result.FileName.Should().Be("test.jpg");
            _photoRepositoryMock.Verify(x => x.GetByIdAsync(photoId, true, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetPhotoByIdAsync_ShouldReturnNull_WhenPhotoDoesNotExist()
        {
            // Arrange
            var photoId = Guid.NewGuid();

            _photoRepositoryMock.Setup(x => x.GetByIdAsync(photoId, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Photo?)null);

            // Act
            var result = await _sut.GetPhotoByIdAsync(photoId);

            // Assert
            result.Should().BeNull();
            _photoRepositoryMock.Verify(x => x.GetByIdAsync(photoId, true, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region UploadPhotoAsync

        [Fact]
        public async Task UploadPhotoAsync_ShouldUploadPhoto_WhenUserIsOwner()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = TestDataFactory.CreateUser("testuser", userId);
            var album = TestDataFactory.CreateAlbum("Test Album", user);
            var dto = TestDataFactory.CreatePhotoUploadDto(album.Id, "test.jpg", "Test Photo");

            _albumRepositoryMock.Setup(x => x.GetByIdAsync(album.Id, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(album);

            _fileStorageMock.Setup(x => x.SavePhotoAsync(It.IsAny<Stream>(), "test.jpg", album.Id))
                .ReturnsAsync(("/photos/test.jpg", "/thumbnails/test.jpg"));

            var createdPhoto = TestDataFactory.CreatePhoto("test.jpg", album.Id, album: album);
            createdPhoto.Title = "Test Photo";

            _photoRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Photo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdPhoto);

            _fileStorageMock.Setup(x => x.GetPhotoUrl(It.IsAny<string>()))
                .Returns((string path) => $"http://localhost{path}");
            _fileStorageMock.Setup(x => x.GetThumbnailUrl(It.IsAny<string>()))
                .Returns((string path) => $"http://localhost{path}");

            // Act
            var result = await _sut.UploadPhotoAsync(dto, userId);

            // Assert
            result.Should().NotBeNull();
            result.FileName.Should().Be("test.jpg");
            result.Title.Should().Be("Test Photo");
            result.AlbumId.Should().Be(album.Id);
            _fileStorageMock.Verify(x => x.SavePhotoAsync(It.IsAny<Stream>(), "test.jpg", album.Id), Times.Once);
            _photoRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Photo>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UploadPhotoAsync_ShouldThrowKeyNotFoundException_WhenAlbumDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var albumId = Guid.NewGuid();
            var dto = TestDataFactory.CreatePhotoUploadDto(albumId);

            _albumRepositoryMock.Setup(x => x.GetByIdAsync(albumId, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Album?)null);

            // Act
            Func<Task> act = async () => await _sut.UploadPhotoAsync(dto, userId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Album not found");
            _fileStorageMock.Verify(x => x.SavePhotoAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task UploadPhotoAsync_ShouldThrowUnauthorizedAccessException_WhenUserIsNotOwner()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var owner = TestDataFactory.CreateUser("owner", ownerId);
            var album = TestDataFactory.CreateAlbum("Test Album", owner);
            var dto = TestDataFactory.CreatePhotoUploadDto(album.Id);

            _albumRepositoryMock.Setup(x => x.GetByIdAsync(album.Id, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(album);

            // Act
            Func<Task> act = async () => await _sut.UploadPhotoAsync(dto, otherUserId);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You don't have permission to upload photos to this album");
            _fileStorageMock.Verify(x => x.SavePhotoAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region DeletePhotoAsync

        [Fact]
        public async Task DeletePhotoAsync_ShouldDeletePhoto_WhenUserIsOwner()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = TestDataFactory.CreateUser("testuser", userId);
            var album = TestDataFactory.CreateAlbum("Test Album", user);
            var photoId = Guid.NewGuid();
            var photo = TestDataFactory.CreatePhoto("test.jpg", album.Id, photoId, album);

            _photoRepositoryMock.Setup(x => x.GetByIdAsync(photoId, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(photo);

            // Act
            var result = await _sut.DeletePhotoAsync(photoId, userId, false);

            // Assert
            result.Should().BeTrue();
            _fileStorageMock.Verify(x => x.DeletePhotoAsync(photo.FilePath, photo.ThumbnailPath), Times.Once);
            _photoRepositoryMock.Verify(x => x.DeleteAsync(photo, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeletePhotoAsync_ShouldDeletePhoto_WhenUserIsAdmin()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var owner = TestDataFactory.CreateUser("owner", ownerId);
            var album = TestDataFactory.CreateAlbum("Test Album", owner);
            var photoId = Guid.NewGuid();
            var photo = TestDataFactory.CreatePhoto("test.jpg", album.Id, photoId, album);

            _photoRepositoryMock.Setup(x => x.GetByIdAsync(photoId, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(photo);

            // Act
            var result = await _sut.DeletePhotoAsync(photoId, adminId, true);

            // Assert
            result.Should().BeTrue();
            _fileStorageMock.Verify(x => x.DeletePhotoAsync(photo.FilePath, photo.ThumbnailPath), Times.Once);
            _photoRepositoryMock.Verify(x => x.DeleteAsync(photo, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeletePhotoAsync_ShouldThrowUnauthorizedAccessException_WhenUserIsNotOwnerAndNotAdmin()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var owner = TestDataFactory.CreateUser("owner", ownerId);
            var album = TestDataFactory.CreateAlbum("Test Album", owner);
            var photoId = Guid.NewGuid();
            var photo = TestDataFactory.CreatePhoto("test.jpg", album.Id, photoId, album);

            _photoRepositoryMock.Setup(x => x.GetByIdAsync(photoId, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(photo);

            // Act
            Func<Task> act = async () => await _sut.DeletePhotoAsync(photoId, otherUserId, false);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You don't have permission to delete this photo");
            _photoRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Photo>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeletePhotoAsync_ShouldReturnFalse_WhenPhotoDoesNotExist()
        {
            // Arrange
            var photoId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _photoRepositoryMock.Setup(x => x.GetByIdAsync(photoId, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Photo?)null);

            // Act
            var result = await _sut.DeletePhotoAsync(photoId, userId, false);

            // Assert
            result.Should().BeFalse();
            _photoRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Photo>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region ToggleLikeAsync

        [Fact]
        public async Task ToggleLikeAsync_ShouldAddLike_WhenNoExistingLike()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var photoId = Guid.NewGuid();
            var user = TestDataFactory.CreateUser("testuser");
            var album = TestDataFactory.CreateAlbum("Test Album", user);
            var photo = TestDataFactory.CreatePhoto("test.jpg", album.Id, photoId, album);

            _photoRepositoryMock.Setup(x => x.GetByIdAsync(photoId, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(photo);
            _photoRepositoryMock.Setup(x => x.GetLikeAsync(photoId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PhotoLike?)null);

            _fileStorageMock.Setup(x => x.GetPhotoUrl(It.IsAny<string>()))
                .Returns((string path) => $"http://localhost{path}");
            _fileStorageMock.Setup(x => x.GetThumbnailUrl(It.IsAny<string>()))
                .Returns((string path) => $"http://localhost{path}");

            // Act
            var result = await _sut.ToggleLikeAsync(photoId, userId, true);

            // Assert
            result.Should().NotBeNull();
            _photoRepositoryMock.Verify(x => x.AddLikeAsync(It.Is<PhotoLike>(l =>
                l.PhotoId == photoId && l.UserId == userId && l.IsLike),
                It.IsAny<CancellationToken>()), Times.Once);
            _photoRepositoryMock.Verify(x => x.RemoveLikeAsync(It.IsAny<PhotoLike>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ToggleLikeAsync_ShouldRemoveLike_WhenSameLikeExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var photoId = Guid.NewGuid();
            var user = TestDataFactory.CreateUser("testuser");
            var album = TestDataFactory.CreateAlbum("Test Album", user);
            var photo = TestDataFactory.CreatePhoto("test.jpg", album.Id, photoId, album);
            var existingLike = TestDataFactory.CreatePhotoLike(photoId, userId, true);

            _photoRepositoryMock.Setup(x => x.GetByIdAsync(photoId, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(photo);
            _photoRepositoryMock.Setup(x => x.GetLikeAsync(photoId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingLike);

            _fileStorageMock.Setup(x => x.GetPhotoUrl(It.IsAny<string>()))
                .Returns((string path) => $"http://localhost{path}");
            _fileStorageMock.Setup(x => x.GetThumbnailUrl(It.IsAny<string>()))
                .Returns((string path) => $"http://localhost{path}");

            // Act
            var result = await _sut.ToggleLikeAsync(photoId, userId, true);

            // Assert
            result.Should().NotBeNull();
            _photoRepositoryMock.Verify(x => x.RemoveLikeAsync(existingLike, It.IsAny<CancellationToken>()), Times.Once);
            _photoRepositoryMock.Verify(x => x.AddLikeAsync(It.IsAny<PhotoLike>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ToggleLikeAsync_ShouldUpdateLike_WhenDifferentLikeExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var photoId = Guid.NewGuid();
            var user = TestDataFactory.CreateUser("testuser");
            var album = TestDataFactory.CreateAlbum("Test Album", user);
            var photo = TestDataFactory.CreatePhoto("test.jpg", album.Id, photoId, album);
            var existingLike = TestDataFactory.CreatePhotoLike(photoId, userId, false); // Dislike

            _photoRepositoryMock.Setup(x => x.GetByIdAsync(photoId, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(photo);
            _photoRepositoryMock.Setup(x => x.GetLikeAsync(photoId, userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingLike);

            _fileStorageMock.Setup(x => x.GetPhotoUrl(It.IsAny<string>()))
                .Returns((string path) => $"http://localhost{path}");
            _fileStorageMock.Setup(x => x.GetThumbnailUrl(It.IsAny<string>()))
                .Returns((string path) => $"http://localhost{path}");

            // Act
            var result = await _sut.ToggleLikeAsync(photoId, userId, true); // Change to Like

            // Assert
            result.Should().NotBeNull();
            existingLike.IsLike.Should().BeTrue();
            _photoRepositoryMock.Verify(x => x.UpdateLikeAsync(existingLike, It.IsAny<CancellationToken>()), Times.Once);
            _photoRepositoryMock.Verify(x => x.AddLikeAsync(It.IsAny<PhotoLike>(), It.IsAny<CancellationToken>()), Times.Never);
            _photoRepositoryMock.Verify(x => x.RemoveLikeAsync(It.IsAny<PhotoLike>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ToggleLikeAsync_ShouldThrowKeyNotFoundException_WhenPhotoDoesNotExist()
        {
            // Arrange
            var photoId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _photoRepositoryMock.Setup(x => x.GetByIdAsync(photoId, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Photo?)null);

            // Act
            Func<Task> act = async () => await _sut.ToggleLikeAsync(photoId, userId, true);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                .WithMessage("Photo not found");
            _photoRepositoryMock.Verify(x => x.AddLikeAsync(It.IsAny<PhotoLike>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion
    }
}