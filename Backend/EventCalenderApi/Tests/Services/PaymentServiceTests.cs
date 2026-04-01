using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Payment;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.Services;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Tests.Services;

public class PaymentServiceTests
{
    private readonly Mock<IRepository<int, Event>> _eventRepoMock = new();
    private readonly Mock<IRepository<int, EventRegistration>> _registrationRepoMock = new();
    private readonly Mock<IRepository<int, Payment>> _paymentRepoMock = new();
    private readonly Mock<IRepository<int, User>> _userRepoMock = new();
    private readonly Mock<IAuditLogRepository> _auditRepoMock = new();
    private readonly Mock<IWalletService> _walletSvcMock = new();
    private readonly PaymentService _sut;

    public PaymentServiceTests()
    {
        _sut = new PaymentService(
            _eventRepoMock.Object,
            _registrationRepoMock.Object,
            _paymentRepoMock.Object,
            _userRepoMock.Object,
            _auditRepoMock.Object,
            _walletSvcMock.Object);

        _auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());
        _walletSvcMock.Setup(s => s.CreditAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
    }

    private static Event PaidApprovedEvent() => new Event
    {
        EventId = 1,
        Title = "Tech Conf",
        IsPaidEvent = true,
        TicketPrice = 500f,
        CommissionPercentage = 10f,
        Status = EventStatus.ACTIVE,
        ApprovalStatus = ApprovalStatus.APPROVED,
        EventDate = DateTime.UtcNow.Date.AddDays(5),
        StartTime = new TimeSpan(9, 0, 0),
        EndTime = new TimeSpan(17, 0, 0),
        CreatedByUserId = 10,
        ApprovedByUserId = 99
    };

    // ── CreatePaymentAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task CreatePaymentAsync_Should_ThrowNotFound_When_EventDoesNotExist()
    {
        // Arrange
        _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 99 }));
    }

    [Fact]
    public async Task CreatePaymentAsync_Should_ThrowBadRequest_When_UserNotRegistered()
    {
        // Arrange
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(PaidApprovedEvent());
        _registrationRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<EventRegistration>().BuildMock());

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 1 }));
    }

    [Fact]
    public async Task CreatePaymentAsync_Should_ThrowBadRequest_When_EventIsNotPaid()
    {
        // Arrange
        var ev = PaidApprovedEvent();
        ev.IsPaidEvent = false;
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
        _registrationRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<EventRegistration>
            {
                new EventRegistration { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED }
            }.BuildMock());

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 1 }));
    }

    [Fact]
    public async Task CreatePaymentAsync_Should_ThrowBadRequest_When_EventNotActive()
    {
        // Arrange
        var ev = PaidApprovedEvent();
        ev.Status = EventStatus.CANCELLED;
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
        _registrationRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<EventRegistration>
            {
                new EventRegistration { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED }
            }.BuildMock());

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 1 }));
    }

    [Fact]
    public async Task CreatePaymentAsync_Should_ThrowBadRequest_When_EventNotApproved()
    {
        // Arrange
        var ev = PaidApprovedEvent();
        ev.ApprovalStatus = ApprovalStatus.PENDING;
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
        _registrationRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<EventRegistration>
            {
                new EventRegistration { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED }
            }.BuildMock());

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 1 }));
    }

    [Fact]
    public async Task CreatePaymentAsync_Should_ThrowBadRequest_When_AlreadyPaidAndStillRegistered()
    {
        // Arrange
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(PaidApprovedEvent());
        _registrationRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<EventRegistration>
            {
                new EventRegistration { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED }
            }.BuildMock());
        _paymentRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payment>
            {
                new Payment { PaymentId = 1, UserId = 1, EventId = 1, Status = PaymentStatus.SUCCESS }
            }.BuildMock());

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 1 }));
    }

    // ── GetByUserAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserAsync_Should_ReturnUserPayments()
    {
        // Arrange
        var payments = new List<Payment>
        {
            new Payment { PaymentId = 1, UserId = 1, EventId = 1 },
            new Payment { PaymentId = 2, UserId = 2, EventId = 2 }
        };
        _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

        // Act
        var result = (await _sut.GetByUserAsync(1)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].UserId);
    }

    // ── GetByEventAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetByEventAsync_Should_ReturnEventPayments()
    {
        // Arrange
        var payments = new List<Payment>
        {
            new Payment { PaymentId = 1, UserId = 1, EventId = 1 },
            new Payment { PaymentId = 2, UserId = 2, EventId = 2 }
        };
        _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

        // Act
        var result = (await _sut.GetByEventAsync(1)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].EventId);
    }

    // ── GetAllPaymentsAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetAllPaymentsAsync_Should_ReturnAllPayments()
    {
        // Arrange
        var payments = new List<Payment>
        {
            new Payment { PaymentId = 1 },
            new Payment { PaymentId = 2 }
        };
        _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

        // Act
        var result = (await _sut.GetAllPaymentsAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
    }

    // ── RefundAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task RefundAsync_Should_ThrowNotFound_When_PaymentDoesNotExist()
    {
        // Arrange
        _paymentRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Payment?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.RefundAsync(99));
    }

    [Fact]
    public async Task RefundAsync_Should_ThrowBadRequest_When_AlreadyRefunded()
    {
        // Arrange
        _paymentRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Payment { PaymentId = 1, Status = PaymentStatus.REFUNDED });

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.RefundAsync(1));
    }

    [Fact]
    public async Task RefundAsync_Should_ThrowNotFound_When_EventDoesNotExist()
    {
        // Arrange
        _paymentRepoMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Payment { PaymentId = 1, EventId = 99, Status = PaymentStatus.SUCCESS });
        _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.RefundAsync(1));
    }

    // ── GetCommissionSummaryAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetCommissionSummaryAsync_Should_ReturnCorrectSummary()
    {
        // Arrange
        var payments = new List<Payment>
        {
            new Payment { PaymentId = 1, Status = PaymentStatus.SUCCESS, CommissionAmount = 50f, OrganizerAmount = 450f },
            new Payment { PaymentId = 2, Status = PaymentStatus.SUCCESS, CommissionAmount = 30f, OrganizerAmount = 270f },
            new Payment { PaymentId = 3, Status = PaymentStatus.REFUNDED, CommissionAmount = 10f, OrganizerAmount = 90f }
        };
        _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

        // Act
        var result = await _sut.GetCommissionSummaryAsync();

        // Assert
        Assert.Equal(80f, result.TotalCommission);
        Assert.Equal(720f, result.TotalOrganizerPayout);
        Assert.Equal(2, result.TotalPayments);
    }

    // ── GetOrganizerEarningsAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetOrganizerEarningsAsync_Should_ReturnOrganizerEarnings()
    {
        // Arrange
        var payments = new List<Payment>
        {
            new Payment { PaymentId = 1, Status = PaymentStatus.SUCCESS, AmountPaid = 500f, CommissionAmount = 50f, OrganizerAmount = 450f, Event = new Event { CreatedByUserId = 1 } },
            new Payment { PaymentId = 2, Status = PaymentStatus.SUCCESS, AmountPaid = 300f, CommissionAmount = 30f, OrganizerAmount = 270f, Event = new Event { CreatedByUserId = 2 } }
        };
        _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

        // Act
        var result = await _sut.GetOrganizerEarningsAsync(organizerId: 1);

        // Assert
        Assert.Equal(500f, result.TotalRevenue);
        Assert.Equal(50f, result.TotalCommission);
        Assert.Equal(450f, result.NetEarnings);
        Assert.Equal(1, result.TotalTransactions);
    }

    // ── GetEventWiseEarningsAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetEventWiseEarningsAsync_Should_GroupByEvent()
    {
        // Arrange
        var ev = new Event { EventId = 1, Title = "Tech Conf", CreatedByUserId = 1 };
        var payments = new List<Payment>
        {
            new Payment { PaymentId = 1, EventId = 1, Status = PaymentStatus.SUCCESS, AmountPaid = 500f, CommissionAmount = 50f, OrganizerAmount = 450f, Event = ev },
            new Payment { PaymentId = 2, EventId = 1, Status = PaymentStatus.SUCCESS, AmountPaid = 500f, CommissionAmount = 50f, OrganizerAmount = 450f, Event = ev }
        };
        _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

        // Act
        var result = (await _sut.GetEventWiseEarningsAsync(organizerId: 1)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(2, result[0].TotalTransactions);
        Assert.Equal(1000f, result[0].TotalRevenue);
    }

    // ── GetOrganizerRefundsPagedAsync ──────────────────────────────────────

    [Fact]
    public async Task GetOrganizerRefundsPagedAsync_Should_ReturnPagedRefunds()
    {
        // Arrange
        var ev = new Event { EventId = 1, CreatedByUserId = 1 };
        var payments = Enumerable.Range(1, 6).Select(i => new Payment
        {
            PaymentId = i, Status = PaymentStatus.REFUNDED, Event = ev
        }).ToList();
        _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

        // Act
        var result = await _sut.GetOrganizerRefundsPagedAsync(organizerId: 1, pageNumber: 1, pageSize: 4);

        // Assert
        Assert.Equal(6, result.TotalRecords);
        Assert.Equal(4, result.Data.Count());
    }
}

