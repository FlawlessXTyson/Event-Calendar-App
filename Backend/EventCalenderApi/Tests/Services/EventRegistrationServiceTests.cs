using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.EventRegisration;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.Services;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Tests.Services;

public class EventRegistrationServiceTests
{
    private readonly Mock<IRepository<int, EventRegistration>> _registrationRepoMock = new();
    private readonly Mock<IRepository<int, Event>> _eventRepoMock = new();
    private readonly Mock<IRepository<int, Payment>> _paymentRepoMock = new();
    private readonly Mock<IRepository<int, RefundRequest>> _refundRepoMock = new();
    private readonly Mock<IAuditLogRepository> _auditRepoMock = new();
    private readonly Mock<IWalletService> _walletSvcMock = new();
    private readonly EventRegistrationService _sut;

    public EventRegistrationServiceTests()
    {
        _sut = new EventRegistrationService(
            _registrationRepoMock.Object,
            _eventRepoMock.Object,
            _paymentRepoMock.Object,
            _refundRepoMock.Object,
            _auditRepoMock.Object,
            _walletSvcMock.Object);

        _auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());
        _walletSvcMock.Setup(s => s.CreditAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    private static Event ApprovedActiveEvent(int id = 1) => new Event
    {
        EventId = id,
        Title = "Tech Conf",
        EventDate = DateTime.UtcNow.Date.AddDays(5),
        StartTime = new TimeSpan(9, 0, 0),
        EndTime = new TimeSpan(17, 0, 0),
        Status = EventStatus.ACTIVE,
        ApprovalStatus = ApprovalStatus.APPROVED,
        CreatedByUserId = 10
    };

    // ── RegisterAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_Should_ReturnRegistration_When_ValidRequest()
    {
        // Arrange
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ApprovedActiveEvent());
        _registrationRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<EventRegistration>().BuildMock());
        _registrationRepoMock.Setup(r => r.AddAsync(It.IsAny<EventRegistration>()))
            .ReturnsAsync((EventRegistration reg) => { reg.RegistrationId = 1; return reg; });

        var dto = new EventRegisterationRequestDTO { EventId = 1 };

        // Act
        var result = await _sut.RegisterAsync(dto, userId: 1);

        // Assert
        Assert.Equal(RegistrationStatus.REGISTERED, result.Status);
        Assert.Equal(1, result.EventId);
    }

    [Fact]
    public async Task RegisterAsync_Should_ThrowNotFound_When_EventDoesNotExist()
    {
        // Arrange
        _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.RegisterAsync(new EventRegisterationRequestDTO { EventId = 99 }, userId: 1));
    }

    [Fact]
    public async Task RegisterAsync_Should_ThrowBadRequest_When_EventNotApproved()
    {
        // Arrange
        var ev = ApprovedActiveEvent();
        ev.ApprovalStatus = ApprovalStatus.PENDING;
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.RegisterAsync(new EventRegisterationRequestDTO { EventId = 1 }, userId: 1));
    }

    [Fact]
    public async Task RegisterAsync_Should_ThrowBadRequest_When_EventNotActive()
    {
        // Arrange
        var ev = ApprovedActiveEvent();
        ev.Status = EventStatus.CANCELLED;
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.RegisterAsync(new EventRegisterationRequestDTO { EventId = 1 }, userId: 1));
    }

    [Fact]
    public async Task RegisterAsync_Should_ThrowBadRequest_When_RegistrationDeadlinePassed()
    {
        // Arrange
        var ev = ApprovedActiveEvent();
        ev.RegistrationDeadline = DateTime.UtcNow.AddHours(-1);
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.RegisterAsync(new EventRegisterationRequestDTO { EventId = 1 }, userId: 1));
    }

    [Fact]
    public async Task RegisterAsync_Should_ThrowBadRequest_When_NoSeatsAvailable()
    {
        // Arrange
        var ev = ApprovedActiveEvent();
        ev.SeatsLimit = 1;
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
        _registrationRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<EventRegistration>
            {
                new EventRegistration { EventId = 1, UserId = 99, Status = RegistrationStatus.REGISTERED }
            }.BuildMock());

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.RegisterAsync(new EventRegisterationRequestDTO { EventId = 1 }, userId: 1));
    }

    [Fact]
    public async Task RegisterAsync_Should_ThrowBadRequest_When_AlreadyRegistered()
    {
        // Arrange
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ApprovedActiveEvent());
        _registrationRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<EventRegistration>
            {
                new EventRegistration { EventId = 1, UserId = 1, Status = RegistrationStatus.REGISTERED }
            }.BuildMock());

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.RegisterAsync(new EventRegisterationRequestDTO { EventId = 1 }, userId: 1));
    }

    // ── CancelAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_Should_ThrowNotFound_When_RegistrationDoesNotExist()
    {
        // Arrange
        _registrationRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((EventRegistration?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.CancelAsync(99, userId: 1, role: "USER"));
    }

    [Fact]
    public async Task CancelAsync_Should_ThrowBadRequest_When_AlreadyCancelled()
    {
        // Arrange
        _registrationRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new EventRegistration { RegistrationId = 1, UserId = 1, Status = RegistrationStatus.CANCELLED });

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CancelAsync(1, userId: 1, role: "USER"));
    }

    [Fact]
    public async Task CancelAsync_Should_ThrowUnauthorized_When_UserCancelsOtherRegistration()
    {
        // Arrange
        _registrationRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new EventRegistration { RegistrationId = 1, UserId = 2, Status = RegistrationStatus.REGISTERED, EventId = 1 });
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ApprovedActiveEvent());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.CancelAsync(1, userId: 1, role: "USER"));
    }

    [Fact]
    public async Task CancelAsync_Should_CancelRegistration_When_NoPaymentExists()
    {
        // Arrange
        var reg = new EventRegistration { RegistrationId = 1, UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED };
        _registrationRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reg);
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ApprovedActiveEvent());
        _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());
        _registrationRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<EventRegistration>())).ReturnsAsync(reg);

        // Act
        var result = await _sut.CancelAsync(1, userId: 1, role: "USER");

        // Assert
        _registrationRepoMock.Verify(r => r.UpdateAsync(1, It.IsAny<EventRegistration>()), Times.Once);
    }

    // ── GetByEventAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByEventAsync_Should_ReturnRegistrationsForEvent()
    {
        // Arrange
        var regs = new List<EventRegistration>
        {
            new EventRegistration { RegistrationId = 1, EventId = 1, UserId = 1 },
            new EventRegistration { RegistrationId = 2, EventId = 2, UserId = 2 }
        };
        _registrationRepoMock.Setup(r => r.GetQueryable()).Returns(regs.BuildMock());

        // Act
        var result = (await _sut.GetByEventAsync(1)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].EventId);
    }

    // ── GetMyRegistrationsAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetMyRegistrationsAsync_Should_ReturnActiveRegistrationsForUser()
    {
        // Arrange
        var regs = new List<EventRegistration>
        {
            new EventRegistration { RegistrationId = 1, UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED },
            new EventRegistration { RegistrationId = 2, UserId = 1, EventId = 2, Status = RegistrationStatus.CANCELLED },
            new EventRegistration { RegistrationId = 3, UserId = 2, EventId = 3, Status = RegistrationStatus.REGISTERED }
        };
        _registrationRepoMock.Setup(r => r.GetQueryable()).Returns(regs.BuildMock());

        // Act
        var result = (await _sut.GetMyRegistrationsAsync(1)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(RegistrationStatus.REGISTERED, result[0].Status);
    }

    // ── GetByEventPagedAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetByEventPagedAsync_Should_ReturnPagedResult()
    {
        // Arrange
        var regs = Enumerable.Range(1, 10)
            .Select(i => new EventRegistration { RegistrationId = i, EventId = 1, UserId = i })
            .ToList();
        _registrationRepoMock.Setup(r => r.GetQueryable()).Returns(regs.BuildMock());

        // Act
        var result = await _sut.GetByEventPagedAsync(1, pageNumber: 1, pageSize: 5, filterDate: null);

        // Assert
        Assert.Equal(10, result.TotalRecords);
        Assert.Equal(5, result.Data.Count());
    }
}

