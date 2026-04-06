using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Services;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Tests.Services;

public class RefundRequestServiceTests
{
    private readonly Mock<IRepository<int, RefundRequest>> _refundRepoMock = new();
    private readonly Mock<IRepository<int, Payment>> _paymentRepoMock = new();
    private readonly Mock<IAuditLogRepository> _auditRepoMock = new();
    private readonly RefundRequestService _sut;

    public RefundRequestServiceTests()
    {
        _sut = new RefundRequestService(_refundRepoMock.Object, _paymentRepoMock.Object, _auditRepoMock.Object);
        _auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());
    }

    private static Payment SamplePayment(int userId = 1, PaymentStatus status = PaymentStatus.SUCCESS) => new Payment
    {
        PaymentId = 10,
        UserId = userId,
        EventId = 5,
        AmountPaid = 500f,
        CommissionAmount = 50f,
        OrganizerAmount = 450f,
        Status = status,
        Event = new Event { EventId = 5, Title = "Tech Conf" },
        User = new User { UserId = userId, Name = "Alice" }
    };

    // ── CreateAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_Should_ReturnRefundRequest_When_ValidRequest()
    {
        // Arrange
        var payment = SamplePayment();
        _paymentRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payment> { payment }.BuildMock());
        _refundRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<RefundRequest>().BuildMock());

        var createdReq = new RefundRequest
        {
            RefundRequestId = 1, UserId = 1, EventId = 5, PaymentId = 10,
            Status = RefundRequestStatus.PENDING,
            User = new User { UserId = 1, Name = "Alice" },
            Event = new Event { EventId = 5, Title = "Tech Conf" },
            Payment = payment
        };
        _refundRepoMock.Setup(r => r.AddAsync(It.IsAny<RefundRequest>())).ReturnsAsync(createdReq);

        // BuildDTO calls GetQueryable again — return the created request
        var callCount = 0;
        _refundRepoMock.Setup(r => r.GetQueryable()).Returns(() =>
        {
            callCount++;
            return callCount == 1
                ? new List<RefundRequest>().BuildMock()
                : new List<RefundRequest> { createdReq }.BuildMock();
        });

        // Act
        var result = await _sut.CreateAsync(userId: 1, paymentId: 10);

        // Assert
        Assert.Equal(RefundRequestStatus.PENDING, result.Status);
        Assert.Equal(10, result.PaymentId);
    }

    [Fact]
    public async Task CreateAsync_Should_ThrowNotFound_When_PaymentDoesNotExist()
    {
        // Arrange
        _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(1, 99));
    }

    [Fact]
    public async Task CreateAsync_Should_ThrowUnauthorized_When_PaymentBelongsToOtherUser()
    {
        // Arrange
        var payment = SamplePayment(userId: 2);
        _paymentRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payment> { payment }.BuildMock());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => _sut.CreateAsync(userId: 1, paymentId: 10));
    }

    [Fact]
    public async Task CreateAsync_Should_ThrowBadRequest_When_PaymentAlreadyRefunded()
    {
        // Arrange
        var payment = SamplePayment(status: PaymentStatus.REFUNDED);
        _paymentRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payment> { payment }.BuildMock());

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateAsync(1, 10));
    }

    [Fact]
    public async Task CreateAsync_Should_ThrowBadRequest_When_PendingRequestAlreadyExists()
    {
        // Arrange
        var payment = SamplePayment();
        _paymentRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Payment> { payment }.BuildMock());

        var existing = new RefundRequest { RefundRequestId = 1, PaymentId = 10, Status = RefundRequestStatus.PENDING };
        _refundRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<RefundRequest> { existing }.BuildMock());

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.CreateAsync(1, 10));
    }

    // ── GetPendingAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetPendingAsync_Should_ReturnOnlyPendingRequests()
    {
        // Arrange
        var requests = new List<RefundRequest>
        {
            new RefundRequest { RefundRequestId = 1, Status = RefundRequestStatus.PENDING },
            new RefundRequest { RefundRequestId = 2, Status = RefundRequestStatus.APPROVED }
        };
        _refundRepoMock.Setup(r => r.GetQueryable()).Returns(requests.BuildMock());

        // Act
        var result = (await _sut.GetPendingAsync()).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(RefundRequestStatus.PENDING, result[0].Status);
    }

    // ── ApproveAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveAsync_Should_ThrowBadRequest_When_PercentageOutOfRange()
    {
        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.ApproveAsync(1, adminId: 1, percentage: 110f));
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.ApproveAsync(1, adminId: 1, percentage: -5f));
    }

    [Fact]
    public async Task ApproveAsync_Should_ThrowNotFound_When_RequestDoesNotExist()
    {
        // Arrange
        _refundRepoMock.Setup(r => r.GetQueryable()).Returns(new List<RefundRequest>().BuildMock());

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.ApproveAsync(99, adminId: 1, percentage: 100f));
    }

    [Fact]
    public async Task ApproveAsync_Should_ThrowBadRequest_When_RequestAlreadyProcessed()
    {
        // Arrange
        var req = new RefundRequest
        {
            RefundRequestId = 1,
            Status = RefundRequestStatus.APPROVED,
            Payment = SamplePayment()
        };
        _refundRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<RefundRequest> { req }.BuildMock());

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.ApproveAsync(1, adminId: 1, percentage: 100f));
    }

    // ── RejectAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task RejectAsync_Should_ThrowNotFound_When_RequestDoesNotExist()
    {
        // Arrange
        _refundRepoMock.Setup(r => r.GetQueryable()).Returns(new List<RefundRequest>().BuildMock());

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.RejectAsync(99, adminId: 1));
    }

    [Fact]
    public async Task RejectAsync_Should_ThrowBadRequest_When_RequestAlreadyProcessed()
    {
        // Arrange
        var req = new RefundRequest { RefundRequestId = 1, Status = RefundRequestStatus.REJECTED };
        _refundRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<RefundRequest> { req }.BuildMock());

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.RejectAsync(1, adminId: 1));
    }

    [Fact]
    public async Task RejectAsync_Should_RejectRequest_When_RequestIsPending()
    {
        // Arrange
        var req = new RefundRequest
        {
            RefundRequestId = 1,
            Status = RefundRequestStatus.PENDING,
            User = new User { UserId = 1, Name = "Alice" },
            Event = new Event { EventId = 5, Title = "Tech Conf" },
            Payment = SamplePayment()
        };
        _refundRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<RefundRequest> { req }.BuildMock());
        _refundRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<RefundRequest>())).ReturnsAsync(req);

        // Act
        var result = await _sut.RejectAsync(1, adminId: 99);

        // Assert
        Assert.Equal(RefundRequestStatus.REJECTED, result.Status);
        _refundRepoMock.Verify(r => r.UpdateAsync(1, It.IsAny<RefundRequest>()), Times.Once);
    }
}

