using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.AuditLog;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Services;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Tests.Services;

public class AuditLogServiceTests
{
    private readonly Mock<IAuditLogRepository> _repoMock = new();
    private readonly Mock<IRepository<int, User>> _userRepoMock = new();
    private readonly AuditLogService _sut;

    public AuditLogServiceTests()
    {
        _sut = new AuditLogService(_repoMock.Object, _userRepoMock.Object);
    }

    private static List<AuditLog> SampleLogs() =>
    [
        new AuditLog { Id = 1, UserId = 1, Role = "USER", Action = "LOGIN", Entity = "User", EntityId = 1, CreatedAt = DateTime.UtcNow },
        new AuditLog { Id = 2, UserId = 2, Role = "ADMIN", Action = "APPROVE_EVENT", Entity = "Event", EntityId = 5, CreatedAt = DateTime.UtcNow }
    ];

    private static List<User> SampleUsers() =>
    [
        new User { UserId = 1, Name = "Alice" },
        new User { UserId = 2, Name = "Bob" }
    ];

    private void SetupMocks(List<AuditLog> logs, List<User> users)
    {
        _repoMock.Setup(r => r.GetQueryable()).Returns(logs.BuildMock());
        _userRepoMock.Setup(r => r.GetQueryable()).Returns(users.BuildMock());
    }

    [Fact]
    public async Task GetAllAsync_Should_ReturnAllLogs_When_LogsExist()
    {
        // Arrange
        var logs = SampleLogs();
        var users = SampleUsers();
        SetupMocks(logs, users);

        // Act
        var result = (await _sut.GetAllAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Alice", result.First(r => r.UserId == 1).UserName);
        Assert.Equal("Bob", result.First(r => r.UserId == 2).UserName);
    }

    [Fact]
    public async Task GetAllAsync_Should_ReturnEmptyUserName_When_UserNotFound()
    {
        // Arrange
        SetupMocks(SampleLogs(), []);

        // Act
        var result = (await _sut.GetAllAsync()).ToList();

        // Assert
        Assert.All(result, r => Assert.Equal(string.Empty, r.UserName));
    }

    [Fact]
    public async Task GetByUserIdAsync_Should_ReturnOnlyMatchingLogs_When_UserIdProvided()
    {
        // Arrange
        SetupMocks(SampleLogs(), SampleUsers());

        // Act
        var result = (await _sut.GetByUserIdAsync(1)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].UserId);
    }

    [Fact]
    public async Task GetByEntityAsync_Should_ReturnMatchingLogs_When_EntityMatches()
    {
        // Arrange
        SetupMocks(SampleLogs(), SampleUsers());

        // Act
        var result = (await _sut.GetByEntityAsync("event")).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("Event", result[0].Entity);
    }

    [Fact]
    public async Task GetByActionAsync_Should_ReturnMatchingLogs_When_ActionMatches()
    {
        // Arrange
        SetupMocks(SampleLogs(), SampleUsers());

        // Act
        var result = (await _sut.GetByActionAsync("login")).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("LOGIN", result[0].Action);
    }

    [Fact]
    public async Task GetByActionAsync_Should_ReturnEmpty_When_NoMatchingAction()
    {
        // Arrange
        SetupMocks(SampleLogs(), SampleUsers());

        // Act
        var result = (await _sut.GetByActionAsync("NONEXISTENT")).ToList();

        // Assert
        Assert.Empty(result);
    }
}

