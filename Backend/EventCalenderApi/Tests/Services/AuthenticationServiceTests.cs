using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Services;
using Microsoft.Extensions.Configuration;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Tests.Services;

public class AuthenticationServiceTests
{
    private readonly Mock<IRepository<int, User>> _userRepoMock = new();
    private readonly Mock<IAuditLogRepository> _auditRepoMock = new();
    private readonly IConfiguration _configuration;
    private readonly AuthenticationService _sut;

    public AuthenticationServiceTests()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "super-secret-key-that-is-long-enough-for-hmac256",
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience",
            ["Jwt:DurationInMinutes"] = "60"
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _sut = new AuthenticationService(_userRepoMock.Object, _configuration, _auditRepoMock.Object);
    }

    // ── RegisterAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_Should_ReturnToken_When_ValidRequest()
    {
        // Arrange
        var request = new RegisterRequestDTO { Email = "test@example.com", Password = "Password1!", UserName = "TestUser" };
        var emptyUsers = new List<User>();
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(emptyUsers.BuildMock());
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { u.UserId = 1; return u; });
        _auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        Assert.NotNull(result.Token);
        Assert.NotEmpty(result.Token);
    }

    [Theory]
    [InlineData("", "Password1!")]
    [InlineData("test@example.com", "")]
    [InlineData("  ", "Password1!")]
    public async Task RegisterAsync_Should_ThrowBadRequest_When_EmailOrPasswordMissing(string email, string password)
    {
        // Arrange
        var request = new RegisterRequestDTO { Email = email, Password = password };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_Should_ThrowBadRequest_When_EmailAlreadyRegistered()
    {
        // Arrange
        var existing = new List<User> { new User { UserId = 1, Email = "test@example.com" } };
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(existing.BuildMock());
        var request = new RegisterRequestDTO { Email = "test@example.com", Password = "Password1!" };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.RegisterAsync(request));
    }

    // ── LoginAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_Should_ReturnToken_When_ValidCredentials()
    {
        // Arrange
        var hash = BCrypt.Net.BCrypt.HashPassword("Password1!");
        var users = new List<User>
        {
            new User { UserId = 1, Email = "test@example.com", PasswordHash = hash, Status = AccountStatus.ACTIVE, Role = UserRole.USER }
        };
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.BuildMock());
        _auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

        var request = new LoginRequestDTO { Email = "test@example.com", Password = "Password1!" };

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.NotNull(result.Token);
    }

    [Theory]
    [InlineData("", "Password1!")]
    [InlineData("test@example.com", "")]
    public async Task LoginAsync_Should_ThrowBadRequest_When_CredentialsMissing(string email, string password)
    {
        // Arrange
        var request = new LoginRequestDTO { Email = email, Password = password };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_Should_ThrowUnauthorized_When_UserNotFound()
    {
        // Arrange
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(new List<User>().BuildMock());
        var request = new LoginRequestDTO { Email = "notfound@example.com", Password = "Password1!" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_Should_ThrowUnauthorized_When_WrongPassword()
    {
        // Arrange
        var hash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword");
        var users = new List<User>
        {
            new User { UserId = 1, Email = "test@example.com", PasswordHash = hash, Status = AccountStatus.ACTIVE }
        };
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.BuildMock());
        var request = new LoginRequestDTO { Email = "test@example.com", Password = "WrongPassword" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_Should_ThrowUnauthorized_When_AccountBlocked()
    {
        // Arrange
        var hash = BCrypt.Net.BCrypt.HashPassword("Password1!");
        var users = new List<User>
        {
            new User { UserId = 1, Email = "test@example.com", PasswordHash = hash, Status = AccountStatus.BLOCKED }
        };
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.BuildMock());
        var request = new LoginRequestDTO { Email = "test@example.com", Password = "Password1!" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.LoginAsync(request));
    }
}

