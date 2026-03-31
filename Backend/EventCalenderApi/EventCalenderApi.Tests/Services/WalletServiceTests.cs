using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Wallet;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.Services;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace EventCalenderApi.Tests.Services
{
    public class WalletServiceTests
    {
        private readonly Mock<IRepository<int, Wallet>> _walletRepoMock = new();
        private readonly Mock<IRepository<int, WalletTransaction>> _txRepoMock = new();
        private readonly Mock<IRepository<int, Payment>> _paymentRepoMock = new();
        private readonly Mock<IRepository<int, Event>> _eventRepoMock = new();
        private readonly Mock<IRepository<int, EventRegistration>> _regRepoMock = new();
        private readonly Mock<IAuditLogRepository> _auditMock = new();

        private WalletService CreateService() => new(
            _walletRepoMock.Object,
            _txRepoMock.Object,
            _paymentRepoMock.Object,
            _eventRepoMock.Object,
            _regRepoMock.Object,
            _auditMock.Object);

        private void SetupWallet(Wallet? wallet)
        {
            var list = wallet != null ? new List<Wallet> { wallet } : new List<Wallet>();
            _walletRepoMock.Setup(r => r.GetQueryable()).Returns(list.BuildMock());
        }

        // ── GetOrCreateWalletAsync ───────────────────────────────────────

        [Fact]
        public async Task GetOrCreateWallet_Existing_ReturnsExisting()
        {
            var wallet = new Wallet { WalletId = 1, UserId = 1, Balance = 500f };
            SetupWallet(wallet);

            var result = await CreateService().GetOrCreateWalletAsync(1);
            Assert.Equal(500f, result.Balance);
        }

        [Fact]
        public async Task GetOrCreateWallet_NotExisting_CreatesNew()
        {
            SetupWallet(null);
            _walletRepoMock.Setup(r => r.AddAsync(It.IsAny<Wallet>()))
                .ReturnsAsync(new Wallet { WalletId = 1, UserId = 1, Balance = 0 });

            var result = await CreateService().GetOrCreateWalletAsync(1);
            Assert.Equal(0f, result.Balance);
        }

        // ── AddMoneyAsync ────────────────────────────────────────────────

        [Fact]
        public async Task AddMoney_ValidAmount_IncreasesBalance()
        {
            var wallet = new Wallet { WalletId = 1, UserId = 1, Balance = 100f };
            SetupWallet(wallet);
            _walletRepoMock.Setup(r => r.UpdateAsync(1, wallet)).ReturnsAsync(wallet);
            _txRepoMock.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>()))
                .ReturnsAsync(new WalletTransaction());

            var result = await CreateService().AddMoneyAsync(1, new AddMoneyRequestDTO { Amount = 200f, PaymentMethod = "upi" });
            Assert.Equal(300f, result.Balance);
        }

        [Fact]
        public async Task AddMoney_ZeroAmount_ThrowsBadRequest()
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().AddMoneyAsync(1, new AddMoneyRequestDTO { Amount = 0 }));
        }

        [Fact]
        public async Task AddMoney_NegativeAmount_ThrowsBadRequest()
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().AddMoneyAsync(1, new AddMoneyRequestDTO { Amount = -50f }));
        }

        // ── CreditAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task Credit_PositiveAmount_IncreasesBalance()
        {
            var wallet = new Wallet { WalletId = 1, UserId = 1, Balance = 100f };
            SetupWallet(wallet);
            _walletRepoMock.Setup(r => r.UpdateAsync(1, wallet)).ReturnsAsync(wallet);
            _txRepoMock.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>())).ReturnsAsync(new WalletTransaction());

            await CreateService().CreditAsync(1, 50f, "REFUND", "Test refund");
            Assert.Equal(150f, wallet.Balance);
        }

        [Fact]
        public async Task Credit_ZeroAmount_DoesNothing()
        {
            var svc = CreateService();
            await svc.CreditAsync(1, 0f, "REFUND", "No-op");
            _walletRepoMock.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Wallet>()), Times.Never);
        }

        [Theory]
        [InlineData("COMMISSION", WalletTransactionSource.COMMISSION)]
        [InlineData("COMPENSATION", WalletTransactionSource.COMPENSATION)]
        [InlineData("ORGANIZER_EARNING", WalletTransactionSource.ORGANIZER_EARNING)]
        [InlineData("ADD_MONEY", WalletTransactionSource.ADD_MONEY)]
        [InlineData("REFUND", WalletTransactionSource.REFUND)]
        public async Task Credit_MapsSourceCorrectly(string source, WalletTransactionSource expected)
        {
            var wallet = new Wallet { WalletId = 1, UserId = 1, Balance = 0f };
            SetupWallet(wallet);
            _walletRepoMock.Setup(r => r.UpdateAsync(1, wallet)).ReturnsAsync(wallet);

            WalletTransaction? captured = null;
            _txRepoMock.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>()))
                .Callback<WalletTransaction>(t => captured = t)
                .ReturnsAsync(new WalletTransaction());

            await CreateService().CreditAsync(1, 10f, source, "desc");
            Assert.Equal(expected, captured?.Source);
        }

        // ── DebitAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task Debit_AllowsNegativeBalance()
        {
            var wallet = new Wallet { WalletId = 1, UserId = 1, Balance = 10f };
            SetupWallet(wallet);
            _walletRepoMock.Setup(r => r.UpdateAsync(1, wallet)).ReturnsAsync(wallet);
            _txRepoMock.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>())).ReturnsAsync(new WalletTransaction());

            await CreateService().DebitAsync(1, 50f, "REFUND", "System refund");
            Assert.Equal(-40f, wallet.Balance);
        }

        [Fact]
        public async Task Debit_ZeroAmount_DoesNothing()
        {
            await CreateService().DebitAsync(1, 0f, "REFUND", "No-op");
            _walletRepoMock.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Wallet>()), Times.Never);
        }

        // ── DebitStrictAsync ─────────────────────────────────────────────

        [Fact]
        public async Task DebitStrict_SufficientBalance_Debits()
        {
            var wallet = new Wallet { WalletId = 1, UserId = 1, Balance = 200f };
            SetupWallet(wallet);
            _walletRepoMock.Setup(r => r.UpdateAsync(1, wallet)).ReturnsAsync(wallet);
            _txRepoMock.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>())).ReturnsAsync(new WalletTransaction());

            await CreateService().DebitStrictAsync(1, 100f, "PAYMENT", "Test");
            Assert.Equal(100f, wallet.Balance);
        }

        [Fact]
        public async Task DebitStrict_InsufficientBalance_ThrowsBadRequest()
        {
            var wallet = new Wallet { WalletId = 1, UserId = 1, Balance = 50f };
            SetupWallet(wallet);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().DebitStrictAsync(1, 100f, "PAYMENT", "Test"));
        }

        [Fact]
        public async Task DebitStrict_ZeroAmount_DoesNothing()
        {
            await CreateService().DebitStrictAsync(1, 0f, "PAYMENT", "No-op");
            _walletRepoMock.Verify(r => r.UpdateAsync(It.IsAny<int>(), It.IsAny<Wallet>()), Times.Never);
        }

        // ── GetTransactionsAsync ─────────────────────────────────────────

        [Fact]
        public async Task GetTransactions_ReturnsUserTransactions()
        {
            var txs = new List<WalletTransaction>
            {
                new() { TransactionId = 1, UserId = 1, Amount = 100f, CreatedAt = DateTime.UtcNow },
                new() { TransactionId = 2, UserId = 2, Amount = 50f, CreatedAt = DateTime.UtcNow }
            };
            _txRepoMock.Setup(r => r.GetQueryable()).Returns(txs.BuildMock());

            var result = (await CreateService().GetTransactionsAsync(1)).ToList();
            Assert.Single(result);
            Assert.Equal(1, result[0].UserId);
        }

        // ── PayWithWalletAsync ───────────────────────────────────────────

        [Fact]
        public async Task PayWithWallet_Success_CreatesPayment()
        {
            var ev = new Event
            {
                EventId = 1,
                Title = "Test",
                IsPaidEvent = true,
                ApprovalStatus = ApprovalStatus.APPROVED,
                Status = EventStatus.ACTIVE,
                TicketPrice = 100f,
                CommissionPercentage = 10f,
                EventDate = DateTime.UtcNow.Date.AddDays(1),
                StartTime = new TimeSpan(10, 0, 0)
            };
            var wallet = new Wallet { WalletId = 1, UserId = 1, Balance = 500f };

            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            SetupWallet(wallet);
            _walletRepoMock.Setup(r => r.UpdateAsync(1, wallet)).ReturnsAsync(wallet);

            var registrations = new List<EventRegistration>
            {
                new() { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(registrations.BuildMock());

            var noPayments = new List<Payment>();
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(noPayments.BuildMock());
            _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                .ReturnsAsync(new Payment { PaymentId = 1, UserId = 1, EventId = 1, AmountPaid = 100f, Status = PaymentStatus.SUCCESS });

            _txRepoMock.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>())).ReturnsAsync(new WalletTransaction());
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().PayWithWalletAsync(1, new WalletPaymentRequestDTO { EventId = 1 });
            Assert.Equal(PaymentStatus.SUCCESS, result.Status);
        }

        [Fact]
        public async Task PayWithWallet_EventNotFound_ThrowsNotFound()
        {
            _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);
            await Assert.ThrowsAsync<NotFoundException>(() =>
                CreateService().PayWithWalletAsync(1, new WalletPaymentRequestDTO { EventId = 99 }));
        }

        [Fact]
        public async Task PayWithWallet_FreeEvent_ThrowsBadRequest()
        {
            var ev = new Event { EventId = 1, IsPaidEvent = false };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().PayWithWalletAsync(1, new WalletPaymentRequestDTO { EventId = 1 }));
        }

        [Fact]
        public async Task PayWithWallet_NotApproved_ThrowsBadRequest()
        {
            var ev = new Event { EventId = 1, IsPaidEvent = true, ApprovalStatus = ApprovalStatus.PENDING };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().PayWithWalletAsync(1, new WalletPaymentRequestDTO { EventId = 1 }));
        }

        [Fact]
        public async Task PayWithWallet_NotActive_ThrowsBadRequest()
        {
            var ev = new Event { EventId = 1, IsPaidEvent = true, ApprovalStatus = ApprovalStatus.APPROVED, Status = EventStatus.CANCELLED };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().PayWithWalletAsync(1, new WalletPaymentRequestDTO { EventId = 1 }));
        }

        [Fact]
        public async Task PayWithWallet_NotRegistered_ThrowsBadRequest()
        {
            var ev = new Event
            {
                EventId = 1,
                IsPaidEvent = true,
                ApprovalStatus = ApprovalStatus.APPROVED,
                Status = EventStatus.ACTIVE,
                EventDate = DateTime.UtcNow.Date.AddDays(1),
                StartTime = new TimeSpan(10, 0, 0)
            };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _regRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<EventRegistration>().BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().PayWithWalletAsync(1, new WalletPaymentRequestDTO { EventId = 1 }));
        }

        [Fact]
        public async Task PayWithWallet_AlreadyPaid_ThrowsBadRequest()
        {
            var ev = new Event
            {
                EventId = 1,
                IsPaidEvent = true,
                ApprovalStatus = ApprovalStatus.APPROVED,
                Status = EventStatus.ACTIVE,
                EventDate = DateTime.UtcNow.Date.AddDays(1),
                StartTime = new TimeSpan(10, 0, 0)
            };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            var registrations = new List<EventRegistration>
            {
                new() { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(registrations.BuildMock());

            var payments = new List<Payment>
            {
                new() { UserId = 1, EventId = 1, Status = PaymentStatus.SUCCESS }
            };
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().PayWithWalletAsync(1, new WalletPaymentRequestDTO { EventId = 1 }));
        }

        [Fact]
        public async Task PayWithWallet_InsufficientBalance_ThrowsBadRequest()
        {
            var ev = new Event
            {
                EventId = 1,
                IsPaidEvent = true,
                ApprovalStatus = ApprovalStatus.APPROVED,
                Status = EventStatus.ACTIVE,
                TicketPrice = 500f,
                EventDate = DateTime.UtcNow.Date.AddDays(1),
                StartTime = new TimeSpan(10, 0, 0)
            };
            var wallet = new Wallet { WalletId = 1, UserId = 1, Balance = 100f };

            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            SetupWallet(wallet);

            var registrations = new List<EventRegistration>
            {
                new() { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(registrations.BuildMock());
            _paymentRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Payment>().BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().PayWithWalletAsync(1, new WalletPaymentRequestDTO { EventId = 1 }));
        }

        // ── PayWithWallet — event already started ────────────────────────

        [Fact]
        public async Task PayWithWallet_EventAlreadyStarted_ThrowsBadRequest()
        {
            var ev = new Event
            {
                EventId = 1,
                IsPaidEvent = true,
                ApprovalStatus = ApprovalStatus.APPROVED,
                Status = EventStatus.ACTIVE,
                TicketPrice = 100f,
                EventDate = DateTime.UtcNow.Date.AddDays(-1), // started yesterday
                StartTime = new TimeSpan(10, 0, 0)
            };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().PayWithWalletAsync(1, new WalletPaymentRequestDTO { EventId = 1 }));
        }

        // ── GetTransactions — empty ──────────────────────────────────────

        [Fact]
        public async Task GetTransactions_NoTransactions_ReturnsEmpty()
        {
            _txRepoMock.Setup(r => r.GetQueryable()).Returns(new List<WalletTransaction>().BuildMock());
            var result = await CreateService().GetTransactionsAsync(99);
            Assert.Empty(result);
        }

        // ── AddMoney — creates wallet if not exists ──────────────────────

        [Fact]
        public async Task AddMoney_WalletNotExisting_CreatesAndAdds()
        {
            SetupWallet(null);
            var newWallet = new Wallet { WalletId = 1, UserId = 1, Balance = 0f };
            _walletRepoMock.Setup(r => r.AddAsync(It.IsAny<Wallet>())).ReturnsAsync(newWallet);
            _walletRepoMock.Setup(r => r.UpdateAsync(1, newWallet)).ReturnsAsync(newWallet);
            _txRepoMock.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>())).ReturnsAsync(new WalletTransaction());

            var result = await CreateService().AddMoneyAsync(1, new AddMoneyRequestDTO { Amount = 100f, PaymentMethod = "card" });
            Assert.Equal(100f, result.Balance);
        }

        // ── PayWithWallet — audit log fields ─────────────────────────────

        [Fact]
        public async Task PayWithWallet_AuditLog_HasCorrectAction()
        {
            var ev = new Event
            {
                EventId = 1, Title = "Test",
                IsPaidEvent = true, ApprovalStatus = ApprovalStatus.APPROVED,
                Status = EventStatus.ACTIVE, TicketPrice = 100f, CommissionPercentage = 10f,
                EventDate = DateTime.UtcNow.Date.AddDays(1), StartTime = new TimeSpan(10, 0, 0)
            };
            var wallet = new Wallet { WalletId = 1, UserId = 1, Balance = 500f };

            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            SetupWallet(wallet);
            _walletRepoMock.Setup(r => r.UpdateAsync(1, wallet)).ReturnsAsync(wallet);
            _regRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<EventRegistration> { new() { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED } }.BuildMock());
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());
            _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                .ReturnsAsync(new Payment { PaymentId = 1, UserId = 1, EventId = 1, AmountPaid = 100f, Status = PaymentStatus.SUCCESS });
            _txRepoMock.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>())).ReturnsAsync(new WalletTransaction());

            AuditLog? captured = null;
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>()))
                .Callback<AuditLog>(log => captured = log)
                .ReturnsAsync(new AuditLog());

            await CreateService().PayWithWalletAsync(1, new WalletPaymentRequestDTO { EventId = 1 });

            Assert.Equal("WALLET_PAYMENT", captured?.Action);
            Assert.Equal(1, captured?.UserId);
        }

        // ── AddMoney — transaction description includes method ────────────

        [Fact]
        public async Task AddMoney_TransactionDescription_IncludesPaymentMethod()
        {
            var wallet = new Wallet { WalletId = 1, UserId = 1, Balance = 0f };
            SetupWallet(wallet);
            _walletRepoMock.Setup(r => r.UpdateAsync(1, wallet)).ReturnsAsync(wallet);

            WalletTransaction? captured = null;
            _txRepoMock.Setup(r => r.AddAsync(It.IsAny<WalletTransaction>()))
                .Callback<WalletTransaction>(t => captured = t)
                .ReturnsAsync(new WalletTransaction());

            await CreateService().AddMoneyAsync(1, new AddMoneyRequestDTO { Amount = 50f, PaymentMethod = "upi" });

            Assert.Contains("upi", captured?.Description);
            Assert.Equal(WalletTransactionType.CREDIT, captured?.Type);
            Assert.Equal(WalletTransactionSource.ADD_MONEY, captured?.Source);
        }

        // ── GetOrCreateWallet — maps all DTO fields ──────────────────────

        [Fact]
        public async Task GetOrCreateWallet_MapsAllFields()
        {
            var wallet = new Wallet { WalletId = 5, UserId = 3, Balance = 999f, UpdatedAt = DateTime.UtcNow };
            SetupWallet(wallet);

            var result = await CreateService().GetOrCreateWalletAsync(3);

            Assert.Equal(5, result.WalletId);
            Assert.Equal(3, result.UserId);
            Assert.Equal(999f, result.Balance);
        }
    }
}





