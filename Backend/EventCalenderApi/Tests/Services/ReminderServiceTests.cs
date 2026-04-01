using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Remainder;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Services;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Tests.Services;

public class ReminderServiceTests
{
    private readonly Mock<IRepository<int, Reminder>> _repoMock = new();
    private readonly Mock<IRepository<int, Event>> _eventRepoMock = new();
    private readonly ReminderService _sut;

    public ReminderServiceTests()
    {
        _sut = new ReminderService(_repoMock.Object, _eventRepoMock.Object);
    }

    // ── CreateAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_Should_ReturnReminder_When_ManualDateTimeProvided()
    {
        // Arrange
        var futureTime = DateTime.UtcNow.AddHours(2);
        var dto = new CreateReminderRequestDTO { ReminderTitle = "Meeting", ReminderDateTime = futureTime };
        _repoMock.Setup(r => r.GetQueryable()).Returns(new List<Reminder>().BuildMock());
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Reminder>()))
            .ReturnsAsync((Reminder r) => { r.ReminderId = 1; return r; });

        // Act
        var result = await _sut.CreateAsync(dto, userId: 1);

        // Assert
        Assert.Equal("Meeting", result.ReminderTitle);
        Assert.Equal(1, result.ReminderId);
    }

    [Fact]
    public async Task CreateAsync_Should_ReturnReminder_When_EventBasedMinutesBeforeProvided()
    {
        // Arrange
        var eventDate = DateTime.UtcNow.Date.AddDays(2);
        var startTime = new TimeSpan(10, 0, 0);
        var ev = new Event { EventId = 5, EventDate = eventDate, StartTime = startTime };
        _eventRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(ev);
        _repoMock.Setup(r => r.GetQueryable()).Returns(new List<Reminder>().BuildMock());
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Reminder>()))
            .ReturnsAsync((Reminder r) => { r.ReminderId = 2; return r; });

        var dto = new CreateReminderRequestDTO { ReminderTitle = "Event Reminder", EventId = 5, MinutesBefore = 30 };

        // Act
        var result = await _sut.CreateAsync(dto, userId: 1);

        // Assert
        Assert.Equal("Event Reminder", result.ReminderTitle);
    }

    [Fact]
    public async Task CreateAsync_Should_ThrowBadRequest_When_DtoIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateAsync(null!, userId: 1));
    }

    [Fact]
    public async Task CreateAsync_Should_ThrowBadRequest_When_TitleIsEmpty()
    {
        // Arrange
        var dto = new CreateReminderRequestDTO { ReminderTitle = "  ", ReminderDateTime = DateTime.UtcNow.AddHours(1) };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateAsync(dto, userId: 1));
    }

    [Fact]
    public async Task CreateAsync_Should_ThrowBadRequest_When_BothDateTimeAndMinutesBeforeProvided()
    {
        // Arrange
        var dto = new CreateReminderRequestDTO
        {
            ReminderTitle = "Test",
            ReminderDateTime = DateTime.UtcNow.AddHours(1),
            MinutesBefore = 30
        };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateAsync(dto, userId: 1));
    }

    [Fact]
    public async Task CreateAsync_Should_ThrowBadRequest_When_ManualDateTimeIsInPast()
    {
        // Arrange
        var dto = new CreateReminderRequestDTO { ReminderTitle = "Test", ReminderDateTime = DateTime.UtcNow.AddHours(-1) };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateAsync(dto, userId: 1));
    }

    [Fact]
    public async Task CreateAsync_Should_ThrowBadRequest_When_NeitherDateTimeNorMinutesBeforeProvided()
    {
        // Arrange
        var dto = new CreateReminderRequestDTO { ReminderTitle = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateAsync(dto, userId: 1));
    }

    [Fact]
    public async Task CreateAsync_Should_ThrowBadRequest_When_MinutesBeforeIsZeroOrNegative()
    {
        // Arrange
        var dto = new CreateReminderRequestDTO { ReminderTitle = "Test", EventId = 5, MinutesBefore = 0 };
        _eventRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Event { EventId = 5, EventDate = DateTime.UtcNow.AddDays(1), StartTime = new TimeSpan(10, 0, 0) });

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateAsync(dto, userId: 1));
    }

    [Fact]
    public async Task CreateAsync_Should_ThrowNotFound_When_EventDoesNotExist()
    {
        // Arrange
        _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);
        var dto = new CreateReminderRequestDTO { ReminderTitle = "Test", EventId = 99, MinutesBefore = 30 };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(dto, userId: 1));
    }

    // ── GetByUserAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserAsync_Should_ReturnUserReminders()
    {
        // Arrange
        var reminders = new List<Reminder>
        {
            new Reminder { ReminderId = 1, UserId = 1, ReminderTitle = "R1" },
            new Reminder { ReminderId = 2, UserId = 2, ReminderTitle = "R2" }
        };
        _repoMock.Setup(r => r.GetQueryable()).Returns(reminders.BuildMock());

        // Act
        var result = (await _sut.GetByUserAsync(1)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("R1", result[0].ReminderTitle);
    }

    // ── DeleteAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_Should_Delete_When_ReminderBelongsToUser()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Reminder { ReminderId = 1, UserId = 1 });
        _repoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(new Reminder { ReminderId = 1 });

        // Act
        await _sut.DeleteAsync(1, userId: 1);

        // Assert
        _repoMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Should_ThrowNotFound_When_ReminderDoesNotExist()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Reminder?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(99, userId: 1));
    }

    [Fact]
    public async Task DeleteAsync_Should_ThrowUnauthorized_When_ReminderBelongsToOtherUser()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Reminder { ReminderId = 1, UserId = 2 });

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.DeleteAsync(1, userId: 1));
    }

    // ── GetDueRemindersAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetDueRemindersAsync_Should_ReturnDueReminders_When_WithinWindow()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var reminders = new List<Reminder>
        {
            new Reminder { ReminderId = 1, UserId = 1, ReminderTitle = "Due Now", ReminderDateTime = now.AddSeconds(-10) },
            new Reminder { ReminderId = 2, UserId = 1, ReminderTitle = "Future", ReminderDateTime = now.AddHours(2) }
        };
        _repoMock.Setup(r => r.GetQueryable()).Returns(reminders.BuildMock());

        // Act
        var result = (await _sut.GetDueRemindersAsync(1)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("Due Now", result[0].ReminderTitle);
    }
}

