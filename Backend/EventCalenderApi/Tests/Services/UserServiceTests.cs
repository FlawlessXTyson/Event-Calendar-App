using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.User;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Services;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IRepository<int, User>> _userRepoMock = new();
    private readonly Mock<IAuditLogRepository> _auditRepoMock = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_userRepoMock.Object, _auditRepoMock.Object);
        _auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());
    }

    // ── CreateUserAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_Should_ReturnUser_When_ValidRequest()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(new List<User>().BuildMock());
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { u.UserId = 1; return u; });

        var request = new CreateUserRequestDTO { Name = "Alice", Email = "alice@example.com", Password = "Pass1!", Role = UserRole.USER };

        // Act
        var result = await _sut.CreateUserAsync(request);

        // Assert
        Assert.Equal("alice@example.com", result.Email);
        Assert.Equal("Alice", result.Name);
    }

    [Theory]
    [InlineData("", "Pass1!")]
    [InlineData("alice@example.com", "")]
    public async Task CreateUserAsync_Should_ThrowBadRequest_When_EmailOrPasswordMissing(string email, string password)
    {
        // Arrange
        var request = new CreateUserRequestDTO { Email = email, Password = password };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateUserAsync(request));
    }

    [Fact]
    public async Task CreateUserAsync_Should_ThrowBadRequest_When_EmailAlreadyExists()
    {
        // Arrange
        var users = new List<User> { new User { UserId = 1, Email = "alice@example.com" } };
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.BuildMock());

        var request = new CreateUserRequestDTO { Email = "alice@example.com", Password = "Pass1!" };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateUserAsync(request));
    }

    // ── GetUserByIdAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetUserByIdAsync_Should_ReturnUser_When_UserExists()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new User { UserId = 1, Name = "Alice", Email = "alice@example.com" });

        // Act
        var result = await _sut.GetUserByIdAsync(1);

        // Assert
        Assert.Equal(1, result.UserId);
    }

    [Fact]
    public async Task GetUserByIdAsync_Should_ThrowNotFound_When_UserDoesNotExist()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetUserByIdAsync(99));
    }

    // ── GetAllUsersAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAllUsersAsync_Should_ReturnAllUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User { UserId = 1, Name = "Alice" },
            new User { UserId = 2, Name = "Bob" }
        };
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.BuildMock());

        // Act
        var result = (await _sut.GetAllUsersAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
    }

    // ── UpdateUserAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUserAsync_Should_ReturnUpdatedUser_When_ValidRequest()
    {
        // Arrange
        var user = new User { UserId = 1, Name = "Alice", Email = "alice@example.com" };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(new List<User>().BuildMock());
        _userRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<User>())).ReturnsAsync(user);

        var request = new UpdateUserRequestDTO { Name = "Alice Updated" };

        // Act
        var result = await _sut.UpdateUserAsync(1, request);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateUserAsync_Should_ThrowNotFound_When_UserDoesNotExist()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateUserAsync(99, new UpdateUserRequestDTO()));
    }

    [Fact]
    public async Task UpdateUserAsync_Should_ThrowBadRequest_When_EmailAlreadyInUse()
    {
        // Arrange
        var user = new User { UserId = 1, Email = "alice@example.com" };
        var otherUser = new User { UserId = 2, Email = "taken@example.com" };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(new List<User> { otherUser }.BuildMock());

        var request = new UpdateUserRequestDTO { Email = "taken@example.com" };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.UpdateUserAsync(1, request));
    }

    // ── DeleteUserAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteUserAsync_Should_Complete_When_UserExists()
    {
        // Arrange
        _userRepoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(new User { UserId = 1 });

        // Act
        await _sut.DeleteUserAsync(1);

        // Assert
        _userRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_Should_ThrowNotFound_When_UserDoesNotExist()
    {
        // Arrange
        _userRepoMock.Setup(r => r.DeleteAsync(99)).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteUserAsync(99));
    }

    // ── DisableUserAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task DisableUserAsync_Should_BlockUser_When_UserIsActive()
    {
        // Arrange
        var user = new User { UserId = 2, Status = AccountStatus.ACTIVE };
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(2, It.IsAny<User>())).ReturnsAsync(user);

        // Act
        var result = await _sut.DisableUserAsync(2, adminId: 1);

        // Assert
        Assert.Equal(AccountStatus.BLOCKED, result.Status);
    }

    [Fact]
    public async Task DisableUserAsync_Should_ThrowBadRequest_When_AdminDisablesSelf()
    {
        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.DisableUserAsync(1, adminId: 1));
    }

    [Fact]
    public async Task DisableUserAsync_Should_ThrowBadRequest_When_UserAlreadyBlocked()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new User { UserId = 2, Status = AccountStatus.BLOCKED });

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.DisableUserAsync(2, adminId: 1));
    }

    // ── EnableUserAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task EnableUserAsync_Should_ActivateUser_When_UserIsBlocked()
    {
        // Arrange
        var user = new User { UserId = 2, Status = AccountStatus.BLOCKED };
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(2, It.IsAny<User>())).ReturnsAsync(user);

        // Act
        var result = await _sut.EnableUserAsync(2, adminId: 1);

        // Assert
        Assert.Equal(AccountStatus.ACTIVE, result.Status);
    }

    [Fact]
    public async Task EnableUserAsync_Should_ThrowBadRequest_When_UserAlreadyActive()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new User { UserId = 2, Status = AccountStatus.ACTIVE });

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.EnableUserAsync(2, adminId: 1));
    }

    [Fact]
    public async Task EnableUserAsync_Should_ThrowNotFound_When_UserDoesNotExist()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.EnableUserAsync(99, adminId: 1));
    }
}

