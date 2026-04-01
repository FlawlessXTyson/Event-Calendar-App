using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Wallet;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Services;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Tests.Services;

public class WalletServiceTests
{
    private readonly Mock<IRepository<int, Wallet>> _walletRepoMock = new();
    private readonly Mock<IRepository<int, WalletTransaction>> _txRepoMock = new();
    private readonly Mock<IRepository<int, Payment>> _paymentRepoMock = new();
    private readonly Mock<IRepository<int, Event>> _eventRepoMock = new();
    private readonly Mock<IRepository<int, EventRegistration>> _regRepoMock = new();
    private readonly Mock<IAuditLogRepository> _auditRepoMock = new();
    private readonly WalletService _sut;

    public WalletServiceTests()
    {
        _sut = new WalletService(
            _walletRepoMock.Object,
            _txRepoMock.Object,
            _paymentRepoMock.Object,
            _eventRepoMock.Object,
            _regRepoMock.Object,
            _auditRepoMock.Object);

        _auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());
        _txRepoMock.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>()))
            .ReturnsAsync((WalletTransaction t) => t);
    }

    private Wallet SetupWallet(int userId, float balance)
    {
        var wallet = new Wallet { WalletId = 1, UserId = userId, Balance = balance };
        _walletRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Wallet> { wallet }.BuildMock());
        _walletRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Wallet>())).ReturnsAsync(wallet);
        return wallet;
    }

    // ── GetOrCreateWalletAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetOrCreateWalletAsync_Should_ReturnExistingWallet_When_WalletExists()
    {
        // Arrange
        SetupWallet(1, 200f);

        // Act
        var result = await _sut.GetOrCreateWalletAsync(1);

        // Assert
        Assert.Equal(200f, result.Balance);
        _walletRepoMock.Verify(r => r.AddAsync(It.IsAny<Wallet>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateWalletAsync_Should_CreateWallet_When_WalletDoesNotExist()
    {
        // Arrange
        _walletRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Wallet>().BuildMock());
        _walletRepoMock.Setup(r => r.AddAsync(It.IsAny<Wallet>()))
            .ReturnsAsync((Wallet w) => { w.WalletId = 1; return w; });

        // Act
        var result = await _sut.GetOrCreateWalletAsync(1);

        // Assert
        Assert.Equal(0f, result.Balance);
        _walletRepoMock.Verify(r => r.AddAsync(It.IsAny<Wallet>()), Times.Once);
    }

    // ── AddMoneyAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task AddMoneyAsync_Should_IncreaseBalance_When_ValidAmount()
    {
        // Arrange
        SetupWallet(1, 100f);
        var request = new AddMoneyRequestDTO { Amount = 200f, PaymentMethod = "Card" };

        // Act
        var result = await _sut.AddMoneyAsync(1, request);

        // Assert
        Assert.Equal(300f, result.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task AddMoneyAsync_Should_ThrowBadRequest_When_AmountIsZeroOrNegative(float amount)
    {
        // Arrange
        var request = new AddMoneyRequestDTO { Amount = amount };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.AddMoneyAsync(1, request));
    }

    // ── CreditAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreditAsync_Should_IncreaseBalance_When_AmountIsPositive()
    {
        // Arrange
        SetupWallet(1, 100f);

        // Act
        await _sut.CreditAsync(1, 50f, "COMMISSION", "Test credit");

        // Assert
        _walletRepoMock.Verify(r => r.UpdateAsync(1, It.Is<Wallet>(w => w.Balance == 150f)), Times.Once);
    }

    [Fact]
    public async Task CreditAsync_Should_DoNothing_When_AmountIsZeroOrNegative()
    {
        // Act
        await _sut.CreditAsync(1, 0f, "COMMISSION", "Test");

        // Assert
        _walletRepoMock.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Wallet>()), Times.Never);
    }

    // ── DebitAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DebitAsync_Should_DecreaseBalance_When_AmountIsPositive()
    {
        // Arrange
        SetupWallet(1, 200f);

        // Act
        await _sut.DebitAsync(1, 50f, "PAYMENT", "Test debit");

        // Assert
        _walletRepoMock.Verify(r => r.UpdateAsync(1, It.Is<Wallet>(w => w.Balance == 150f)), Times.Once);
    }

    [Fact]
    public async Task DebitAsync_Should_AllowNegativeBalance_When_SystemInitiated()
    {
        // Arrange
        SetupWallet(1, 10f);

        // Act — should NOT throw even though 100 > 10
        await _sut.DebitAsync(1, 100f, "REFUND", "System refund");

        // Assert
        _walletRepoMock.Verify(r => r.UpdateAsync(1, It.Is<Wallet>(w => w.Balance == -90f)), Times.Once);
    }

    [Fact]
    public async Task DebitAsync_Should_DoNothing_When_AmountIsZeroOrNegative()
    {
        // Act
        await _sut.DebitAsync(1, 0f, "PAYMENT", "Test");

        // Assert
        _walletRepoMock.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Wallet>()), Times.Never);
    }

    // ── DebitStrictAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task DebitStrictAsync_Should_DecreaseBalance_When_SufficientFunds()
    {
        // Arrange
        SetupWallet(1, 500f);

        // Act
        await _sut.DebitStrictAsync(1, 200f, "PAYMENT", "Test");

        // Assert
        _walletRepoMock.Verify(r => r.UpdateAsync(1, It.Is<Wallet>(w => w.Balance == 300f)), Times.Once);
    }

    [Fact]
    public async Task DebitStrictAsync_Should_ThrowBadRequest_When_InsufficientFunds()
    {
        // Arrange
        SetupWallet(1, 50f);

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() => _sut.DebitStrictAsync(1, 200f, "PAYMENT", "Test"));
    }

    [Fact]
    public async Task DebitStrictAsync_Should_DoNothing_When_AmountIsZeroOrNegative()
    {
        // Act
        await _sut.DebitStrictAsync(1, 0f, "PAYMENT", "Test");

        // Assert
        _walletRepoMock.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Wallet>()), Times.Never);
    }

    // ── GetTransactionsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetTransactionsAsync_Should_ReturnUserTransactions()
    {
        // Arrange
        var txs = new List<WalletTransaction>
        {
            new WalletTransaction { TransactionId = 1, UserId = 1, Amount = 100f, Type = WalletTransactionType.CREDIT, Source = WalletTransactionSource.ADD_MONEY },
            new WalletTransaction { TransactionId = 2, UserId = 2, Amount = 50f, Type = WalletTransactionType.DEBIT, Source = WalletTransactionSource.PAYMENT }
        };
        _txRepoMock.Setup(r => r.GetQueryable()).Returns(txs.BuildMock());

        // Act
        var result = (await _sut.GetTransactionsAsync(1)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(100f, result[0].Amount);
    }

    // ── PayWithWalletAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task PayWithWalletAsync_Should_ThrowNotFound_When_EventDoesNotExist()
    {
        // Arrange
        _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.PayWithWalletAsync(1, new WalletPaymentRequestDTO { EventId = 99 }));
    }

    [Fact]
    public async Task PayWithWalletAsync_Should_ThrowBadRequest_When_EventIsNotPaid()
    {
        // Arrange
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Event
        {
            EventId = 1, IsPaidEvent = false, ApprovalStatus = ApprovalStatus.APPROVED,
            Status = EventStatus.ACTIVE, EventDate = DateTime.UtcNow.AddDays(5)
        });

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.PayWithWalletAsync(1, new WalletPaymentRequestDTO { EventId = 1 }));
    }

    [Fact]
    public async Task PayWithWalletAsync_Should_ThrowBadRequest_When_InsufficientBalance()
    {
        // Arrange
        var ev = new Event
        {
            EventId = 1, IsPaidEvent = true, TicketPrice = 500f,
            ApprovalStatus = ApprovalStatus.APPROVED, Status = EventStatus.ACTIVE,
            EventDate = DateTime.UtcNow.AddDays(5), StartTime = new TimeSpan(10, 0, 0),
            CommissionPercentage = 10f
        };
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
        _regRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<EventRegistration>
            {
                new EventRegistration { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED }
            }.BuildMock());
        _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());
        SetupWallet(1, 50f); // insufficient

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.PayWithWalletAsync(1, new WalletPaymentRequestDTO { EventId = 1 }));
    }
}

