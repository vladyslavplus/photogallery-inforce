using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Moq;
using PhotoGallery.Application.Common;
using PhotoGallery.Application.DTOs.Albums;
using PhotoGallery.Application.Interfaces.Repositories;
using PhotoGallery.Application.Interfaces.Services;
using PhotoGallery.Application.Services;
using PhotoGallery.Domain.Entities;
using PhotoGallery.Tests.Helpers;

namespace PhotoGallery.Tests.Services
{
    public class AlbumServiceTests
    {
        private readonly IFixture _fixture;
        private readonly Mock<IAlbumRepository> _albumRepositoryMock;
        private readonly Mock<IFileStorageService> _fileStorageMock;
        private readonly AlbumService _sut;

        public AlbumServiceTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());

            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            _albumRepositoryMock = _fixture.Freeze<Mock<IAlbumRepository>>();
            _fileStorageMock = _fixture.Freeze<Mock<IFileStorageService>>();
            _sut = new AlbumService(_albumRepositoryMock.Object, _fileStorageMock.Object);
        }

        #region GetAllAlbumsAsync

        [Fact]
        public async Task GetAllAlbumsAsync_ShouldReturnPagedAlbums_WhenAlbumsExist()
        {
            // Arrange
            var user = TestDataFactory.CreateUser("testuser");
            var albums = new List<Album>
            {
                TestDataFactory.CreateAlbum("Album 1", user),
                TestDataFactory.CreateAlbum("Album 2", user),
                TestDataFactory.CreateAlbum("Album 3", user)
            };

            var pagedResult = new PagedResult<Album>
            {
                Items = albums,
                TotalCount = 3,
                PageNumber = 1,
                PageSize = 5,
                AdditionalData = new Dictionary<Guid, int>()
            };

            _albumRepositoryMock
                .Setup(x => x.GetAllAsync(1, 5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(pagedResult);

            _fileStorageMock
                .Setup(x => x.GetThumbnailUrl(It.IsAny<string>()))
                .Returns((string path) => $"/thumbnails/{path}");

            // Act
            var result = await _sut.GetAllAlbumsAsync(1, 5);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(3);
            result.TotalCount.Should().Be(3);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(5);
            result.Items[0].UserName.Should().Be("testuser");
            _albumRepositoryMock.Verify(x => x.GetAllAsync(1, 5, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region GetUserAlbumsAsync

        [Fact]
        public async Task GetUserAlbumsAsync_ShouldReturnUserAlbums_WhenUserHasAlbums()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = TestDataFactory.CreateUser("testuser", userId);
            var albums = new List<Album>
            {
                TestDataFactory.CreateAlbum("Album 1", user),
                TestDataFactory.CreateAlbum("Album 2", user)
            };

            var pagedResult = new PagedResult<Album>
            {
                Items = albums,
                TotalCount = 2,
                PageNumber = 1,
                PageSize = 5,
                AdditionalData = new Dictionary<Guid, int>()
            };

            _albumRepositoryMock
                .Setup(x => x.GetByUserIdAsync(userId, 1, 5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _sut.GetUserAlbumsAsync(userId, 1, 5);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.Items.Should().OnlyContain(a => a.UserId == userId);
            _albumRepositoryMock.Verify(x => x.GetByUserIdAsync(userId, 1, 5, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region GetAlbumByIdAsync

        [Fact]
        public async Task GetAlbumByIdAsync_ShouldReturnAlbum_WhenAlbumExists()
        {
            // Arrange
            var albumId = Guid.NewGuid();
            var user = TestDataFactory.CreateUser("testuser");
            var album = TestDataFactory.CreateAlbum("Test Album", user, albumId);

            _albumRepositoryMock
                .Setup(x => x.GetByIdAsync(albumId, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(album);

            // Act
            var result = await _sut.GetAlbumByIdAsync(albumId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(albumId);
            result.UserName.Should().Be("testuser");
            _albumRepositoryMock.Verify(x => x.GetByIdAsync(albumId, false, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAlbumByIdAsync_ShouldReturnNull_WhenAlbumDoesNotExist()
        {
            // Arrange
            var albumId = Guid.NewGuid();
            _albumRepositoryMock
                .Setup(x => x.GetByIdAsync(albumId, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Album?)null);

            // Act
            var result = await _sut.GetAlbumByIdAsync(albumId);

            // Assert
            result.Should().BeNull();
            _albumRepositoryMock.Verify(x => x.GetByIdAsync(albumId, false, It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region CreateAlbumAsync

        [Fact]
        public async Task CreateAlbumAsync_ShouldCreateAlbum_WithValidData()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new AlbumCreateDto
            {
                Title = "Test Album",
                Description = "Test Description"
            };

            var user = TestDataFactory.CreateUser("testuser", userId);
            var createdAlbum = TestDataFactory.CreateAlbum(dto.Title, user);
            createdAlbum.Description = dto.Description;

            _albumRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Album>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdAlbum);

            // Act
            var result = await _sut.CreateAlbumAsync(dto, userId);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be(dto.Title);
            result.Description.Should().Be(dto.Description);
            result.UserId.Should().Be(userId);
            _albumRepositoryMock.Verify(x => x.CreateAsync(
                It.Is<Album>(a => a.Title == dto.Title && a.Description == dto.Description && a.UserId == userId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region UpdateAlbumAsync

        [Fact]
        public async Task UpdateAlbumAsync_ShouldUpdateAlbum_WhenUserIsOwner()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = TestDataFactory.CreateUser("testuser", userId);
            var albumId = Guid.NewGuid();
            var album = TestDataFactory.CreateAlbum("Old Title", user, albumId);
            album.Description = "Old Description";

            var updateDto = new AlbumUpdateDto
            {
                Title = "New Title",
                Description = "New Description"
            };

            _albumRepositoryMock
                .Setup(x => x.GetByIdAsync(albumId, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(album);

            // Act
            var result = await _sut.UpdateAlbumAsync(albumId, updateDto, userId);

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be("New Title");
            result.Description.Should().Be("New Description");
            _albumRepositoryMock.Verify(x => x.UpdateAsync(album, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAlbumAsync_ShouldThrowUnauthorizedAccessException_WhenUserIsNotOwner()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var albumId = Guid.NewGuid();
            var owner = TestDataFactory.CreateUser("owner", ownerId);
            var album = TestDataFactory.CreateAlbum("Test Album", owner, albumId);

            var updateDto = new AlbumUpdateDto { Title = "New Title" };

            _albumRepositoryMock
                .Setup(x => x.GetByIdAsync(albumId, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(album);

            // Act
            Func<Task> act = async () => await _sut.UpdateAlbumAsync(albumId, updateDto, otherUserId);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You don't have permission to update this album");
            _albumRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Album>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAlbumAsync_ShouldReturnNull_WhenAlbumDoesNotExist()
        {
            // Arrange
            var albumId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var updateDto = new AlbumUpdateDto { Title = "New Title" };

            _albumRepositoryMock
                .Setup(x => x.GetByIdAsync(albumId, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Album?)null);

            // Act
            var result = await _sut.UpdateAlbumAsync(albumId, updateDto, userId);

            // Assert
            result.Should().BeNull();
            _albumRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Album>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region DeleteAlbumAsync

        [Fact]
        public async Task DeleteAlbumAsync_ShouldDeleteAlbum_WhenUserIsOwner()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = TestDataFactory.CreateUser("testuser", userId);
            var albumId = Guid.NewGuid();
            var album = TestDataFactory.CreateAlbum("Test Album", user, albumId);

            var photo1 = TestDataFactory.CreatePhoto("photo1.jpg", album.Id);
            var photo2 = TestDataFactory.CreatePhoto("photo2.jpg", album.Id);
            album.Photos = new List<Photo> { photo1, photo2 };

            _albumRepositoryMock
                .Setup(x => x.GetByIdAsync(albumId, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(album);

            // Act
            var result = await _sut.DeleteAlbumAsync(albumId, userId, false);

            // Assert
            result.Should().BeTrue();
            _fileStorageMock.Verify(x => x.DeletePhotoAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
            _albumRepositoryMock.Verify(x => x.DeleteAsync(album, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAlbumAsync_ShouldDeleteAlbum_WhenUserIsAdmin()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var albumId = Guid.NewGuid();
            var owner = TestDataFactory.CreateUser("owner", ownerId);
            var album = TestDataFactory.CreateAlbum("Test Album", owner, albumId);

            _albumRepositoryMock
                .Setup(x => x.GetByIdAsync(albumId, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(album);

            // Act
            var result = await _sut.DeleteAlbumAsync(albumId, adminId, true);

            // Assert
            result.Should().BeTrue();
            _albumRepositoryMock.Verify(x => x.DeleteAsync(album, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAlbumAsync_ShouldThrowUnauthorizedAccessException_WhenUserIsNotOwnerAndNotAdmin()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var albumId = Guid.NewGuid();
            var owner = TestDataFactory.CreateUser("owner", ownerId);
            var album = TestDataFactory.CreateAlbum("Test Album", owner, albumId);

            _albumRepositoryMock
                .Setup(x => x.GetByIdAsync(albumId, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(album);

            // Act
            Func<Task> act = async () => await _sut.DeleteAlbumAsync(albumId, otherUserId, false);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("You don't have permission to delete this album");
            _albumRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Album>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAlbumAsync_ShouldReturnFalse_WhenAlbumDoesNotExist()
        {
            // Arrange
            var albumId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _albumRepositoryMock
                .Setup(x => x.GetByIdAsync(albumId, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Album?)null);

            // Act
            var result = await _sut.DeleteAlbumAsync(albumId, userId, false);

            // Assert
            result.Should().BeFalse();
            _albumRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Album>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion
    }
}