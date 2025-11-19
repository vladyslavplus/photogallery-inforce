using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using PhotoGallery.Application.Interfaces.Services;
using PhotoGallery.Application.Services;
using PhotoGallery.Domain.Entities;
using PhotoGallery.Tests.Helpers;

namespace PhotoGallery.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _userManagerMock = UserManagerMockHelper.CreateMockUserManager();
            _tokenServiceMock = new Mock<ITokenService>();
            _sut = new AuthService(_userManagerMock.Object, _tokenServiceMock.Object);
        }

        #region RegisterAsync

        [Fact]
        public async Task RegisterAsync_ShouldCreateUser_WithValidData()
        {
            // Arrange
            var dto = TestDataFactory.CreateRegisterDto();
            var user = TestDataFactory.CreateUser(dto.UserName);

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
            _userManagerMock.Setup(x => x.FindByNameAsync(dto.UserName)).ReturnsAsync((User?)null);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), dto.Password))
                .ReturnsAsync(TestDataFactory.CreateIdentityResult(true));
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "User"))
                .ReturnsAsync(TestDataFactory.CreateIdentityResult(true));
            _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<User>()))
                .ReturnsAsync(new List<string> { "User" });
            _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IList<string>>()))
                .Returns("test-access-token");

            // Act
            var result = await _sut.RegisterAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().Be("test-access-token");
            _userManagerMock.Verify(x => x.CreateAsync(
                It.Is<User>(u => u.UserName == dto.UserName && u.Email == dto.Email),
                dto.Password), Times.Once);
            _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), "User"), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowInvalidOperationException_WhenEmailAlreadyExists()
        {
            // Arrange
            var dto = TestDataFactory.CreateRegisterDto();
            var existingUser = TestDataFactory.CreateUser(dto.UserName);

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(existingUser);

            // Act
            Func<Task> act = async () => await _sut.RegisterAsync(dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("User with this email already exists");
            _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowInvalidOperationException_WhenUsernameAlreadyExists()
        {
            // Arrange
            var dto = TestDataFactory.CreateRegisterDto();
            var existingUser = TestDataFactory.CreateUser(dto.UserName);

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
            _userManagerMock.Setup(x => x.FindByNameAsync(dto.UserName)).ReturnsAsync(existingUser);

            // Act
            Func<Task> act = async () => await _sut.RegisterAsync(dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("User with this username already exists");
            _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrowInvalidOperationException_WhenUserCreationFails()
        {
            // Arrange
            var dto = TestDataFactory.CreateRegisterDto();

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
            _userManagerMock.Setup(x => x.FindByNameAsync(dto.UserName)).ReturnsAsync((User?)null);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), dto.Password))
                .ReturnsAsync(TestDataFactory.CreateIdentityResult(false, "Password too weak"));

            // Act
            Func<Task> act = async () => await _sut.RegisterAsync(dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Failed to create user: Password too weak");
            _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ShouldDeleteUserAndThrow_WhenRoleAssignmentFails()
        {
            // Arrange
            var dto = TestDataFactory.CreateRegisterDto();

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
            _userManagerMock.Setup(x => x.FindByNameAsync(dto.UserName)).ReturnsAsync((User?)null);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), dto.Password))
                .ReturnsAsync(TestDataFactory.CreateIdentityResult(true));
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "User"))
                .ReturnsAsync(TestDataFactory.CreateIdentityResult(false, "Role not found"));
            _userManagerMock.Setup(x => x.DeleteAsync(It.IsAny<User>()))
                .ReturnsAsync(TestDataFactory.CreateIdentityResult(true));

            // Act
            Func<Task> act = async () => await _sut.RegisterAsync(dto);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Failed to assign role to user");
            _userManagerMock.Verify(x => x.DeleteAsync(It.IsAny<User>()), Times.Once);
        }

        #endregion

        #region LoginAsync

        [Fact]
        public async Task LoginAsync_ShouldReturnTokens_WithValidCredentials()
        {
            // Arrange
            var dto = TestDataFactory.CreateLoginDto();
            var user = TestDataFactory.CreateUser("testuser");
            var refreshToken = TestDataFactory.CreateRefreshToken(user.Id);

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
            _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, It.IsAny<IList<string>>()))
                .Returns("test-access-token");
            _tokenServiceMock.Setup(x => x.GenerateRefreshTokenAsync(user.Id, It.IsAny<string>(), null, default))
                .ReturnsAsync(refreshToken);

            // Act
            var (response, token) = await _sut.LoginAsync(dto, "127.0.0.1");

            // Assert
            response.Should().NotBeNull();
            response.AccessToken.Should().Be("test-access-token");
            token.Should().Be(refreshToken.Token);
            _userManagerMock.Verify(x => x.CheckPasswordAsync(user, dto.Password), Times.Once);
            _tokenServiceMock.Verify(x => x.GenerateRefreshTokenAsync(user.Id, "127.0.0.1", null, default), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowUnauthorizedAccessException_WhenUserNotFound()
        {
            // Arrange
            var dto = TestDataFactory.CreateLoginDto();

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync((User?)null);

            // Act
            Func<Task> act = async () => await _sut.LoginAsync(dto, "127.0.0.1");

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Invalid email or password");
            _userManagerMock.Verify(x => x.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrowUnauthorizedAccessException_WhenPasswordIsInvalid()
        {
            // Arrange
            var dto = TestDataFactory.CreateLoginDto();
            var user = TestDataFactory.CreateUser("testuser");

            _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(false);

            // Act
            Func<Task> act = async () => await _sut.LoginAsync(dto, "127.0.0.1");

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Invalid email or password");
            _tokenServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IList<string>>()), Times.Never);
        }

        #endregion

        #region RefreshTokenAsync

        [Fact]
        public async Task RefreshTokenAsync_ShouldReturnNewTokens_WithValidRefreshToken()
        {
            // Arrange
            var user = TestDataFactory.CreateUser("testuser");
            var oldRefreshToken = TestDataFactory.CreateRefreshToken(user.Id, "old-token");
            oldRefreshToken.User = user;

            var newRefreshToken = TestDataFactory.CreateRefreshToken(user.Id, "new-token");

            _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("old-token", It.IsAny<CancellationToken>()))
                .ReturnsAsync(oldRefreshToken);
            _tokenServiceMock.Setup(x => x.GenerateRefreshTokenAsync(user.Id, "127.0.0.1", "old-token", It.IsAny<CancellationToken>()))
                .ReturnsAsync(newRefreshToken);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });
            _tokenServiceMock.Setup(x => x.GenerateAccessToken(user, It.IsAny<IList<string>>()))
                .Returns("new-access-token");

            // Act
            var (response, token) = await _sut.RefreshTokenAsync("old-token", "127.0.0.1");

            // Assert
            response.Should().NotBeNull();
            response.AccessToken.Should().Be("new-access-token");
            token.Should().Be("new-token");
            _tokenServiceMock.Verify(x => x.RevokeTokenAsync("old-token", "127.0.0.1", "Replaced by new token", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldThrowUnauthorizedAccessException_WhenTokenNotFound()
        {
            // Arrange
            _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("invalid-token", It.IsAny<CancellationToken>()))
                .ReturnsAsync((RefreshToken?)null);

            // Act
            Func<Task> act = async () => await _sut.RefreshTokenAsync("invalid-token", "127.0.0.1");

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Invalid refresh token");
            _tokenServiceMock.Verify(x => x.GenerateRefreshTokenAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldThrowUnauthorizedAccessException_WhenTokenIsExpired()
        {
            // Arrange
            var user = TestDataFactory.CreateUser("testuser");
            var expiredToken = TestDataFactory.CreateRefreshToken(user.Id);
            expiredToken.ExpiresAt = DateTime.UtcNow.AddDays(-1); // Expired

            _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("expired-token", It.IsAny<CancellationToken>()))
                .ReturnsAsync(expiredToken);

            // Act
            Func<Task> act = async () => await _sut.RefreshTokenAsync("expired-token", "127.0.0.1");

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Refresh token is expired or revoked");
        }

        [Fact]
        public async Task RefreshTokenAsync_ShouldThrowUnauthorizedAccessException_WhenTokenIsRevoked()
        {
            // Arrange
            var user = TestDataFactory.CreateUser("testuser");
            var revokedToken = TestDataFactory.CreateRefreshToken(user.Id);
            revokedToken.RevokedAt = DateTime.UtcNow; // Revoked

            _tokenServiceMock.Setup(x => x.GetRefreshTokenAsync("revoked-token", It.IsAny<CancellationToken>()))
                .ReturnsAsync(revokedToken);

            // Act
            Func<Task> act = async () => await _sut.RefreshTokenAsync("revoked-token", "127.0.0.1");

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Refresh token is expired or revoked");
        }

        #endregion
    }
}