using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Event;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.Services;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Tests.Services;

public class EventServiceTests
{
    private readonly Mock<IRepository<int, Event>> _eventRepoMock = new();
    private readonly Mock<IRepository<int, User>> _userRepoMock = new();
    private readonly Mock<IRepository<int, EventRegistration>> _registrationRepoMock = new();
    private readonly Mock<IRepository<int, Payment>> _paymentRepoMock = new();
    private readonly Mock<IAuditLogRepository> _auditRepoMock = new();
    private readonly Mock<IWalletService> _walletSvcMock = new();
    private readonly EventService _sut;

    public EventServiceTests()
    {
        _sut = new EventService(
            _eventRepoMock.Object,
            _userRepoMock.Object,
            _registrationRepoMock.Object,
            _paymentRepoMock.Object,
            _auditRepoMock.Object,
            _walletSvcMock.Object);

        _auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());
        _walletSvcMock.Setup(s => s.CreditAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _walletSvcMock.Setup(s => s.DebitAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    private static CreateEventRequestDTO ValidCreateDto(DateTime? eventDate = null) => new CreateEventRequestDTO
    {
        Title = "Tech Summit",
        Description = "Annual tech event",
        EventDate = eventDate ?? DateTime.UtcNow.Date.AddDays(5),
        StartTime = new TimeSpan(9, 0, 0),
        EndTime = new TimeSpan(17, 0, 0),
        Location = "Hall A",
        CreatedByUserId = 1,
        IsPaidEvent = false
    };

    private static Event SampleEvent(int id = 1) => new Event
    {
        EventId = id,
        Title = "Tech Summit",
        EventDate = DateTime.UtcNow.Date.AddDays(5),
        StartTime = new TimeSpan(9, 0, 0),
        EndTime = new TimeSpan(17, 0, 0),
        Status = EventStatus.ACTIVE,
        ApprovalStatus = ApprovalStatus.APPROVED,
        CreatedByUserId = 1,
        IsPaidEvent = false
    };

    // ── CreateEventAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateEventAsync_Should_ReturnEvent_When_ValidRequest()
    {
        // Arrange
        _eventRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Event>().BuildMock());
        _eventRepoMock.Setup(r => r.AddAsync(It.IsAny<Event>()))
            .ReturnsAsync((Event e) => { e.EventId = 1; return e; });
        _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());
        _registrationRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());

        // Act
        var result = await _sut.CreateEventAsync(ValidCreateDto());

        // Assert
        Assert.Equal("Tech Summit", result.Title);
        Assert.Equal(ApprovalStatus.PENDING, result.ApprovalStatus);
    }

    [Fact]
    public async Task CreateEventAsync_Should_ThrowBadRequest_When_EventDateIsInPast()
    {
        // Arrange
        var dto = ValidCreateDto(DateTime.UtcNow.Date.AddDays(-1));

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateEventAsync(dto));
    }

    [Fact]
    public async Task CreateEventAsync_Should_ThrowBadRequest_When_EndTimeBeforeStartTime()
    {
        // Arrange
        var dto = ValidCreateDto();
        dto.StartTime = new TimeSpan(17, 0, 0);
        dto.EndTime = new TimeSpan(9, 0, 0);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateEventAsync(dto));
    }

    [Fact]
    public async Task CreateEventAsync_Should_ThrowBadRequest_When_DuplicateEventExists()
    {
        // Arrange
        var dto = ValidCreateDto();
        var existing = new Event
        {
            EventId = 1, Title = "tech summit", EventDate = dto.EventDate,
            StartTime = dto.StartTime, CreatedByUserId = 1, Status = EventStatus.ACTIVE
        };
        _eventRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Event> { existing }.BuildMock());

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateEventAsync(dto));
    }

    [Fact]
    public async Task CreateEventAsync_Should_ThrowBadRequest_When_PaidEventHasNoTicketPrice()
    {
        // Arrange
        var dto = ValidCreateDto();
        dto.IsPaidEvent = true;
        dto.TicketPrice = 0;
        _eventRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Event>().BuildMock());

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateEventAsync(dto));
    }

    // ── GetByIdAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_Should_ReturnEvent_When_EventExists()
    {
        // Arrange
        var ev = SampleEvent();
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
        _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());
        _registrationRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());

        // Act
        var result = await _sut.GetByIdAsync(1);

        // Assert
        Assert.Equal(1, result.EventId);
    }

    [Fact]
    public async Task GetByIdAsync_Should_ThrowNotFound_When_EventDoesNotExist()
    {
        // Arrange
        _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }

    // ── DeleteAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_Should_ReturnDeletedEvent_When_EventExists()
    {
        // Arrange
        _eventRepoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(SampleEvent());

        // Act
        var result = await _sut.DeleteAsync(1);

        // Assert
        Assert.Equal(1, result.EventId);
    }

    [Fact]
    public async Task DeleteAsync_Should_ThrowNotFound_When_EventDoesNotExist()
    {
        // Arrange
        _eventRepoMock.Setup(r => r.DeleteAsync(99)).ReturnsAsync((Event?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(99));
    }

    // ── ApproveAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveAsync_Should_SetApprovalStatusToApproved()
    {
        // Arrange
        var ev = SampleEvent();
        ev.ApprovalStatus = ApprovalStatus.PENDING;
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
        _eventRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Event>())).ReturnsAsync(ev);

        // Act
        var result = await _sut.ApproveAsync(1, adminId: 99);

        // Assert
        Assert.Equal(ApprovalStatus.APPROVED, result.ApprovalStatus);
    }

    [Fact]
    public async Task ApproveAsync_Should_ThrowNotFound_When_EventDoesNotExist()
    {
        // Arrange
        _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.ApproveAsync(99, adminId: 1));
    }

    // ── RejectAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task RejectAsync_Should_SetApprovalStatusToRejected()
    {
        // Arrange
        var ev = SampleEvent();
        ev.ApprovalStatus = ApprovalStatus.PENDING;
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
        _eventRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Event>())).ReturnsAsync(ev);

        // Act
        var result = await _sut.RejectAsync(1, adminId: 99);

        // Assert
        Assert.Equal(ApprovalStatus.REJECTED, result.ApprovalStatus);
    }

    // ── SearchAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_Should_ReturnMatchingEvents()
    {
        // Arrange
        var events = new List<Event>
        {
            new Event { EventId = 1, Title = "Tech Summit" },
            new Event { EventId = 2, Title = "Music Festival" }
        };
        _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

        // Act
        var result = (await _sut.SearchAsync("Tech")).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("Tech Summit", result[0].Title);
    }

    // ── GetByDateRangeAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetByDateRangeAsync_Should_ReturnEventsInRange()
    {
        // Arrange
        var start = DateTime.UtcNow.AddDays(1);
        var end = DateTime.UtcNow.AddDays(10);
        var events = new List<Event>
        {
            new Event { EventId = 1, EventDate = DateTime.UtcNow.AddDays(3) },
            new Event { EventId = 2, EventDate = DateTime.UtcNow.AddDays(20) }
        };
        _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

        // Act
        var result = (await _sut.GetByDateRangeAsync(start, end)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].EventId);
    }

    // ── GetMyEventsAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetMyEventsAsync_Should_ReturnOrganizerEvents()
    {
        // Arrange
        var events = new List<Event>
        {
            new Event { EventId = 1, CreatedByUserId = 1 },
            new Event { EventId = 2, CreatedByUserId = 2 }
        };
        _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

        // Act
        var result = (await _sut.GetMyEventsAsync(1)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].EventId);
    }

    // ── GetRefundSummaryAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetRefundSummaryAsync_Should_ReturnCorrectSummary()
    {
        // Arrange
        var payments = new List<Payment>
        {
            new Payment { PaymentId = 1, EventId = 1, Status = PaymentStatus.REFUNDED, RefundedAmount = 200f },
            new Payment { PaymentId = 2, EventId = 1, Status = PaymentStatus.REFUNDED, RefundedAmount = 150f }
        };
        _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

        // Act
        var result = await _sut.GetRefundSummaryAsync(1);

        // Assert
        Assert.Equal(2, result.TotalUsersRefunded);
        Assert.Equal(350f, result.TotalRefundAmount);
    }

    // ── CancelEventAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task CancelEventAsync_Should_ThrowNotFound_When_EventDoesNotExist()
    {
        // Arrange
        _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.CancelEventAsync(99, userId: 1, role: "ADMIN"));
    }

    [Fact]
    public async Task CancelEventAsync_Should_ThrowUnauthorized_When_OrganizerCancelsOtherEvent()
    {
        // Arrange
        var ev = new Event
        {
            EventId = 1, CreatedByUserId = 2,
            EventDate = DateTime.UtcNow.Date.AddDays(5),
            StartTime = new TimeSpan(9, 0, 0),
            Status = EventStatus.ACTIVE
        };
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
        _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.CancelEventAsync(1, userId: 1, role: "ORGANIZER"));
    }

    [Fact]
    public async Task CancelEventAsync_Should_ThrowBadRequest_When_EventAlreadyStarted()
    {
        // Arrange
        var ev = new Event
        {
            EventId = 1, CreatedByUserId = 1,
            EventDate = DateTime.UtcNow.Date.AddDays(-1), // past
            StartTime = new TimeSpan(0, 0, 0),
            Status = EventStatus.ACTIVE
        };
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CancelEventAsync(1, userId: 1, role: "ADMIN"));
    }

    // ── GetPagedAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_Should_ReturnPagedResult()
    {
        // Arrange
        var events = Enumerable.Range(1, 10).Select(i => new Event { EventId = i }).ToList();
        _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

        // Act
        var result = await _sut.GetPagedAsync(pageNumber: 1, pageSize: 5);

        // Assert
        Assert.Equal(10, result.TotalRecords);
        Assert.Equal(5, result.Data.Count());
    }
}

