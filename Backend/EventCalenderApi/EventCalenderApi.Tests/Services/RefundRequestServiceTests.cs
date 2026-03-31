using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Services;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace EventCalenderApi.Tests.Services
{
    public class RefundRequestServiceTests
    {
        private readonly Mock<IRepository<int, RefundRequest>> _refundRepoMock = new();
        private readonly Mock<IRepository<int, Payment>> _paymentRepoMock = new();
        private readonly Mock<IAuditLogRepository> _auditMock = new();

        private RefundRequestService CreateService() =>
            new(_refundRepoMock.Object, _paymentRepoMock.Object, _auditMock.Object);

        private static Payment SamplePayment(int userId = 1, PaymentStatus status = PaymentStatus.SUCCESS) => new()
        {
            PaymentId = 1,
            UserId = userId,
            EventId = 1,
            AmountPaid = 100f,
            CommissionAmount = 10f,
            OrganizerAmount = 90f,
            Status = status,
            Event = new Event { EventId = 1, Title = "Test" },
            User = new User { UserId = userId, Name = "Alice" }
        };

        // ── CreateAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task Create_ValidRequest_ReturnsDTO()
        {
            var payment = SamplePayment();
            var payments = new List<Payment> { payment };
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

            var noExisting = new List<RefundRequest>();
            _refundRepoMock.SetupSequence(r => r.GetQueryable())
                .Returns(noExisting.BuildMock())  // duplicate check
                .Returns(new List<RefundRequest>                       // BuildDTO reload
                {
                    new()
                    {
                        RefundRequestId = 1, UserId = 1, EventId = 1, PaymentId = 1,
                        Status = RefundRequestStatus.PENDING,
                        User = new User { UserId = 1, Name = "Alice" },
                        Event = new Event { EventId = 1, Title = "Test" },
                        Payment = payment
                    }
                }.BuildMock());

            _refundRepoMock.Setup(r => r.AddAsync(It.IsAny<RefundRequest>()))
                .ReturnsAsync(new RefundRequest { RefundRequestId = 1, UserId = 1, EventId = 1, PaymentId = 1 });
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().CreateAsync(1, 1);
            Assert.Equal(1, result.RefundRequestId);
        }

        [Fact]
        public async Task Create_PaymentNotFound_ThrowsNotFound()
        {
            _paymentRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Payment>().BuildMock());

            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().CreateAsync(1, 99));
        }

        [Fact]
        public async Task Create_WrongUser_ThrowsUnauthorized()
        {
            var payment = SamplePayment(userId: 2);
            _paymentRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Payment> { payment }.BuildMock());

            await Assert.ThrowsAsync<UnauthorizedException>(() => CreateService().CreateAsync(1, 1));
        }

        [Fact]
        public async Task Create_AlreadyRefunded_ThrowsBadRequest()
        {
            var payment = SamplePayment(status: PaymentStatus.REFUNDED);
            _paymentRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Payment> { payment }.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(1, 1));
        }

        [Fact]
        public async Task Create_DuplicatePending_ThrowsBadRequest()
        {
            var payment = SamplePayment();
            _paymentRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Payment> { payment }.BuildMock());

            var existing = new List<RefundRequest>
            {
                new() { RefundRequestId = 1, PaymentId = 1, Status = RefundRequestStatus.PENDING }
            };
            _refundRepoMock.Setup(r => r.GetQueryable()).Returns(existing.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(1, 1));
        }

        // ── GetPendingAsync ──────────────────────────────────────────────

        [Fact]
        public async Task GetPending_ReturnsPendingOnly()
        {
            var requests = new List<RefundRequest>
            {
                new() { RefundRequestId = 1, Status = RefundRequestStatus.PENDING, User = new User(), Event = new Event(), Payment = SamplePayment() },
                new() { RefundRequestId = 2, Status = RefundRequestStatus.APPROVED, User = new User(), Event = new Event(), Payment = SamplePayment() }
            };
            _refundRepoMock.Setup(r => r.GetQueryable()).Returns(requests.BuildMock());

            var result = (await CreateService().GetPendingAsync()).ToList();
            Assert.Single(result);
        }

        // ── ApproveAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task Approve_ValidRequest_ApprovesAndRefunds()
        {
            var payment = SamplePayment();
            var req = new RefundRequest
            {
                RefundRequestId = 1,
                UserId = 1,
                EventId = 1,
                PaymentId = 1,
                Status = RefundRequestStatus.PENDING,
                Payment = payment
            };

            _refundRepoMock.SetupSequence(r => r.GetQueryable())
                .Returns(new List<RefundRequest> { req }.BuildMock())  // ApproveAsync fetch
                .Returns(new List<RefundRequest>                                            // BuildDTO reload
                {
                    new()
                    {
                        RefundRequestId = 1, UserId = 1, EventId = 1, PaymentId = 1,
                        Status = RefundRequestStatus.APPROVED,
                        User = new User { UserId = 1, Name = "Alice" },
                        Event = new Event { EventId = 1, Title = "Test" },
                        Payment = payment
                    }
                }.BuildMock());

            _paymentRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Payment>())).ReturnsAsync(payment);
            _refundRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<RefundRequest>())).ReturnsAsync(req);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().ApproveAsync(1, 99, 100f);
            Assert.Equal(RefundRequestStatus.APPROVED, result.Status);
        }

        [Fact]
        public async Task Approve_InvalidPercentage_ThrowsBadRequest()
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().ApproveAsync(1, 99, 150f));
        }

        [Fact]
        public async Task Approve_NegativePercentage_ThrowsBadRequest()
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().ApproveAsync(1, 99, -10f));
        }

        [Fact]
        public async Task Approve_NotFound_ThrowsNotFound()
        {
            _refundRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<RefundRequest>().BuildMock());

            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().ApproveAsync(99, 1, 50f));
        }

        [Fact]
        public async Task Approve_AlreadyProcessed_ThrowsBadRequest()
        {
            var req = new RefundRequest
            {
                RefundRequestId = 1,
                Status = RefundRequestStatus.APPROVED,
                Payment = SamplePayment()
            };
            _refundRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<RefundRequest> { req }.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().ApproveAsync(1, 1, 50f));
        }

        // ── RejectAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task Reject_ValidRequest_Rejects()
        {
            var req = new RefundRequest { RefundRequestId = 1, Status = RefundRequestStatus.PENDING };

            _refundRepoMock.SetupSequence(r => r.GetQueryable())
                .Returns(new List<RefundRequest> { req }.BuildMock())
                .Returns(new List<RefundRequest>
                {
                    new()
                    {
                        RefundRequestId = 1, Status = RefundRequestStatus.REJECTED,
                        User = new User(), Event = new Event(), Payment = SamplePayment()
                    }
                }.BuildMock());

            _refundRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<RefundRequest>())).ReturnsAsync(req);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().RejectAsync(1, 99);
            Assert.Equal(RefundRequestStatus.REJECTED, result.Status);
        }

        [Fact]
        public async Task Reject_NotFound_ThrowsNotFound()
        {
            _refundRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<RefundRequest>().BuildMock());

            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().RejectAsync(99, 1));
        }

        [Fact]
        public async Task Reject_AlreadyProcessed_ThrowsBadRequest()
        {
            var req = new RefundRequest { RefundRequestId = 1, Status = RefundRequestStatus.REJECTED };
            _refundRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<RefundRequest> { req }.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().RejectAsync(1, 1));
        }

        // ── ApproveAsync — partial refund math ───────────────────────────

        [Fact]
        public async Task Approve_PartialRefund_CalculatesCorrectSplit()
        {
            var payment = new Payment
            {
                PaymentId = 1, UserId = 1, EventId = 1,
                AmountPaid = 100f, CommissionAmount = 10f, OrganizerAmount = 90f,
                Status = PaymentStatus.SUCCESS
            };
            var req = new RefundRequest
            {
                RefundRequestId = 1, UserId = 1, EventId = 1, PaymentId = 1,
                Status = RefundRequestStatus.PENDING,
                Payment = payment
            };

            _refundRepoMock.SetupSequence(r => r.GetQueryable())
                .Returns(new List<RefundRequest> { req }.BuildMock())
                .Returns(new List<RefundRequest>
                {
                    new()
                    {
                        RefundRequestId = 1, Status = RefundRequestStatus.APPROVED,
                        ApprovedPercentage = 50f,
                        User = new User(), Event = new Event(), Payment = payment
                    }
                }.BuildMock());

            _paymentRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Payment>())).ReturnsAsync(payment);
            _refundRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<RefundRequest>())).ReturnsAsync(req);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().ApproveAsync(1, 99, 50f);

            // 50% of 100 = 50 refunded; commission portion = 50 * (10/100) = 5
            Assert.Equal(50f, payment.RefundedAmount);
            Assert.Equal(PaymentStatus.REFUNDED, payment.Status);
        }

        [Fact]
        public async Task Approve_ZeroPercentage_IsValid()
        {
            var payment = SamplePayment();
            var req = new RefundRequest
            {
                RefundRequestId = 1, Status = RefundRequestStatus.PENDING, Payment = payment
            };

            _refundRepoMock.SetupSequence(r => r.GetQueryable())
                .Returns(new List<RefundRequest> { req }.BuildMock())
                .Returns(new List<RefundRequest>
                {
                    new()
                    {
                        RefundRequestId = 1, Status = RefundRequestStatus.APPROVED,
                        User = new User(), Event = new Event(), Payment = payment
                    }
                }.BuildMock());

            _paymentRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Payment>())).ReturnsAsync(payment);
            _refundRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<RefundRequest>())).ReturnsAsync(req);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().ApproveAsync(1, 99, 0f);
            Assert.Equal(0f, payment.RefundedAmount);
        }

        // ── GetPendingAsync — empty ──────────────────────────────────────

        [Fact]
        public async Task GetPending_NoPendingRequests_ReturnsEmpty()
        {
            _refundRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<RefundRequest>().BuildMock());

            var result = await CreateService().GetPendingAsync();
            Assert.Empty(result);
        }

        // ── ApproveAsync — zero AmountPaid guard ─────────────────────────

        [Fact]
        public async Task Approve_ZeroAmountPaid_AdminRefundIsZero()
        {
            var payment = new Payment
            {
                PaymentId = 1, UserId = 1, EventId = 1,
                AmountPaid = 0f, CommissionAmount = 0f, OrganizerAmount = 0f,
                Status = PaymentStatus.SUCCESS
            };
            var req = new RefundRequest
            {
                RefundRequestId = 1, Status = RefundRequestStatus.PENDING, Payment = payment
            };

            _refundRepoMock.SetupSequence(r => r.GetQueryable())
                .Returns(new List<RefundRequest> { req }.BuildMock())
                .Returns(new List<RefundRequest>
                {
                    new()
                    {
                        RefundRequestId = 1, Status = RefundRequestStatus.APPROVED,
                        User = new User(), Event = new Event(), Payment = payment
                    }
                }.BuildMock());

            _paymentRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<Payment>())).ReturnsAsync(payment);
            _refundRepoMock.Setup(r => r.UpdateAsync(1, It.IsAny<RefundRequest>())).ReturnsAsync(req);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().ApproveAsync(1, 99, 100f);
            Assert.Equal(RefundRequestStatus.APPROVED, result.Status);
            Assert.Equal(0f, payment.RefundedAmount);
        }

        // ── CreateAsync — audit log fields ───────────────────────────────

        [Fact]
        public async Task Create_AuditLog_HasCorrectAction()
        {
            var payment = SamplePayment();
            _paymentRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Payment> { payment }.BuildMock());

            _refundRepoMock.SetupSequence(r => r.GetQueryable())
                .Returns(new List<RefundRequest>().BuildMock())
                .Returns(new List<RefundRequest>
                {
                    new()
                    {
                        RefundRequestId = 1, UserId = 1, EventId = 1, PaymentId = 1,
                        Status = RefundRequestStatus.PENDING,
                        User = new User { UserId = 1, Name = "Alice" },
                        Event = new Event { EventId = 1, Title = "Test" },
                        Payment = payment
                    }
                }.BuildMock());

            _refundRepoMock.Setup(r => r.AddAsync(It.IsAny<RefundRequest>()))
                .ReturnsAsync(new RefundRequest { RefundRequestId = 1, UserId = 1, EventId = 1, PaymentId = 1 });

            AuditLog? captured = null;
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>()))
                .Callback<AuditLog>(log => captured = log)
                .ReturnsAsync(new AuditLog());

            await CreateService().CreateAsync(1, 1);

            Assert.Equal("REFUND_REQUESTED", captured?.Action);
            Assert.Equal(1, captured?.UserId);
        }
    }
}





