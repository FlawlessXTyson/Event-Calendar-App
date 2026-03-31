using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Payment;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.Services;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace EventCalenderApi.Tests.Services
{
    public class PaymentServiceTests
    {
        private readonly Mock<IRepository<int, Event>> _eventRepoMock = new();
        private readonly Mock<IRepository<int, EventRegistration>> _regRepoMock = new();
        private readonly Mock<IRepository<int, Payment>> _paymentRepoMock = new();
        private readonly Mock<IRepository<int, User>> _userRepoMock = new();
        private readonly Mock<IAuditLogRepository> _auditMock = new();
        private readonly Mock<IWalletService> _walletMock = new();

        private PaymentService CreateService() => new(
            _eventRepoMock.Object,
            _regRepoMock.Object,
            _paymentRepoMock.Object,
            _userRepoMock.Object,
            _auditMock.Object,
            _walletMock.Object);

        private static Event PaidApprovedEvent() => new()
        {
            EventId = 1,
            Title = "Paid Event",
            IsPaidEvent = true,
            TicketPrice = 100f,
            CommissionPercentage = 10f,
            Status = EventStatus.ACTIVE,
            ApprovalStatus = ApprovalStatus.APPROVED,
            EventDate = DateTime.UtcNow.Date.AddDays(2),
            StartTime = new TimeSpan(10, 0, 0),
            CreatedByUserId = 5,
            ApprovedByUserId = 10
        };

        // ── CreatePaymentAsync ───────────────────────────────────────────

        [Fact]
        public async Task CreatePayment_Success_ReturnsPayment()
        {
            var ev = PaidApprovedEvent();
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            var registrations = new List<EventRegistration>
            {
                new() { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(registrations.BuildMock());

            var noPayments = new List<Payment>();
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(noPayments.BuildMock());

            var created = new Payment { PaymentId = 1, UserId = 1, EventId = 1, AmountPaid = 100f, Status = PaymentStatus.SUCCESS };
            _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>())).ReturnsAsync(created);

            _walletMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 1 });
            Assert.Equal(PaymentStatus.SUCCESS, result.Status);
        }

        [Fact]
        public async Task CreatePayment_EventNotFound_ThrowsNotFound()
        {
            _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);
            await Assert.ThrowsAsync<NotFoundException>(() =>
                CreateService().CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 99 }));
        }

        [Fact]
        public async Task CreatePayment_NotRegistered_ThrowsBadRequest()
        {
            var ev = PaidApprovedEvent();
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _regRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<EventRegistration>().BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 1 }));
        }

        [Fact]
        public async Task CreatePayment_FreeEvent_ThrowsBadRequest()
        {
            var ev = new Event { EventId = 1, IsPaidEvent = false, Status = EventStatus.ACTIVE, ApprovalStatus = ApprovalStatus.APPROVED };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            var registrations = new List<EventRegistration>
            {
                new() { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(registrations.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 1 }));
        }

        [Fact]
        public async Task CreatePayment_NotActive_ThrowsBadRequest()
        {
            var ev = new Event { EventId = 1, IsPaidEvent = true, Status = EventStatus.CANCELLED, ApprovalStatus = ApprovalStatus.APPROVED };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            var registrations = new List<EventRegistration>
            {
                new() { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(registrations.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 1 }));
        }

        [Fact]
        public async Task CreatePayment_NotApproved_ThrowsBadRequest()
        {
            var ev = new Event { EventId = 1, IsPaidEvent = true, Status = EventStatus.ACTIVE, ApprovalStatus = ApprovalStatus.PENDING };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            var registrations = new List<EventRegistration>
            {
                new() { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(registrations.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 1 }));
        }

        [Fact]
        public async Task CreatePayment_AlreadyPaid_ThrowsBadRequest()
        {
            var ev = PaidApprovedEvent();
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            var registrations = new List<EventRegistration>
            {
                new() { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(registrations.BuildMock());

            var payments = new List<Payment>
            {
                new() { PaymentId = 1, UserId = 1, EventId = 1, Status = PaymentStatus.SUCCESS }
            };
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 1 }));
        }

        [Fact]
        public async Task CreatePayment_FullyBooked_ThrowsBadRequest()
        {
            var ev = PaidApprovedEvent();
            ev.SeatsLimit = 1;
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            var registrations = new List<EventRegistration>
            {
                new() { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED },
                new() { UserId = 2, EventId = 1, Status = RegistrationStatus.REGISTERED }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(registrations.BuildMock());

            var payments = new List<Payment>
            {
                new() { PaymentId = 1, UserId = 2, EventId = 1, Status = PaymentStatus.SUCCESS }
            };
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 1 }));
        }

        // ── GetByUserAsync ───────────────────────────────────────────────

        [Fact]
        public async Task GetByUser_ReturnsUserPayments()
        {
            var payments = new List<Payment>
            {
                new() { PaymentId = 1, UserId = 1, EventId = 1, PaymentDate = DateTime.UtcNow, Event = new Event { Title = "E1" }, User = new User { Name = "Alice" } },
                new() { PaymentId = 2, UserId = 2, EventId = 1, PaymentDate = DateTime.UtcNow, Event = new Event { Title = "E1" }, User = new User { Name = "Bob" } }
            };
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

            var result = (await CreateService().GetByUserAsync(1)).ToList();
            Assert.Single(result);
        }

        // ── GetByEventAsync ──────────────────────────────────────────────

        [Fact]
        public async Task GetByEvent_ReturnsEventPayments()
        {
            var payments = new List<Payment>
            {
                new() { PaymentId = 1, UserId = 1, EventId = 1, PaymentDate = DateTime.UtcNow, Event = new Event { Title = "E1" }, User = new User() },
                new() { PaymentId = 2, UserId = 2, EventId = 2, PaymentDate = DateTime.UtcNow, Event = new Event { Title = "E2" }, User = new User() }
            };
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

            var result = (await CreateService().GetByEventAsync(1)).ToList();
            Assert.Single(result);
        }

        // ── GetAllPaymentsAsync ──────────────────────────────────────────

        [Fact]
        public async Task GetAllPayments_ReturnsAll()
        {
            var payments = new List<Payment>
            {
                new() { PaymentId = 1, UserId = 1, EventId = 1, PaymentDate = DateTime.UtcNow, Event = new Event(), User = new User() },
                new() { PaymentId = 2, UserId = 2, EventId = 2, PaymentDate = DateTime.UtcNow, Event = new Event(), User = new User() }
            };
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

            var result = (await CreateService().GetAllPaymentsAsync()).ToList();
            Assert.Equal(2, result.Count);
        }

        // ── RefundAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task Refund_Success_ReturnsRefunded()
        {
            var payment = new Payment { PaymentId = 1, UserId = 1, EventId = 1, AmountPaid = 100f, Status = PaymentStatus.SUCCESS };
            var ev = new Event
            {
                EventId = 1,
                EventDate = DateTime.UtcNow.Date.AddDays(2),
                EndTime = new TimeSpan(12, 0, 0)
            };

            _paymentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, payment)).ReturnsAsync(payment);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().RefundAsync(1);
            Assert.Equal(PaymentStatus.REFUNDED, result.Status);
        }

        [Fact]
        public async Task Refund_PaymentNotFound_ThrowsNotFound()
        {
            _paymentRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Payment?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().RefundAsync(99));
        }

        [Fact]
        public async Task Refund_AlreadyRefunded_ThrowsBadRequest()
        {
            var payment = new Payment { PaymentId = 1, Status = PaymentStatus.REFUNDED };
            _paymentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().RefundAsync(1));
        }

        [Fact]
        public async Task Refund_EventNotFound_ThrowsNotFound()
        {
            var payment = new Payment { PaymentId = 1, EventId = 99, Status = PaymentStatus.SUCCESS };
            _paymentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
            _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().RefundAsync(1));
        }

        [Fact]
        public async Task Refund_AfterEventEnded_ThrowsBadRequest()
        {
            var payment = new Payment { PaymentId = 1, EventId = 1, Status = PaymentStatus.SUCCESS };
            var ev = new Event
            {
                EventId = 1,
                EventDate = DateTime.UtcNow.Date.AddDays(-3),
                EndTime = new TimeSpan(12, 0, 0)
            };
            _paymentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().RefundAsync(1));
        }

        // ── GetCommissionSummaryAsync ────────────────────────────────────

        [Fact]
        public async Task GetCommissionSummary_ReturnsCorrectTotals()
        {
            var payments = new List<Payment>
            {
                new() { PaymentId = 1, CommissionAmount = 10f, OrganizerAmount = 90f, Status = PaymentStatus.SUCCESS },
                new() { PaymentId = 2, CommissionAmount = 20f, OrganizerAmount = 80f, Status = PaymentStatus.SUCCESS },
                new() { PaymentId = 3, CommissionAmount = 5f, OrganizerAmount = 45f, Status = PaymentStatus.REFUNDED }
            };
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

            var result = await CreateService().GetCommissionSummaryAsync();
            Assert.Equal(30f, result.TotalCommission);
            Assert.Equal(170f, result.TotalOrganizerPayout);
            Assert.Equal(2, result.TotalPayments);
        }

        // ── GetOrganizerEarningsAsync ────────────────────────────────────

        [Fact]
        public async Task GetOrganizerEarnings_ReturnsCorrectTotals()
        {
            var payments = new List<Payment>
            {
                new() { PaymentId = 1, AmountPaid = 100f, CommissionAmount = 10f, OrganizerAmount = 90f, Status = PaymentStatus.SUCCESS, Event = new Event { CreatedByUserId = 5 } },
                new() { PaymentId = 2, AmountPaid = 200f, CommissionAmount = 20f, OrganizerAmount = 180f, Status = PaymentStatus.SUCCESS, Event = new Event { CreatedByUserId = 5 } }
            };
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

            var result = await CreateService().GetOrganizerEarningsAsync(5);
            Assert.Equal(300f, result.TotalRevenue);
            Assert.Equal(270f, result.NetEarnings);
            Assert.Equal(2, result.TotalTransactions);
        }

        // ── GetEventWiseEarningsAsync ────────────────────────────────────

        [Fact]
        public async Task GetEventWiseEarnings_GroupsByEvent()
        {
            var payments = new List<Payment>
            {
                new() { PaymentId = 1, EventId = 1, AmountPaid = 100f, CommissionAmount = 10f, OrganizerAmount = 90f, Status = PaymentStatus.SUCCESS, Event = new Event { EventId = 1, Title = "E1", CreatedByUserId = 5 } },
                new() { PaymentId = 2, EventId = 1, AmountPaid = 100f, CommissionAmount = 10f, OrganizerAmount = 90f, Status = PaymentStatus.SUCCESS, Event = new Event { EventId = 1, Title = "E1", CreatedByUserId = 5 } },
                new() { PaymentId = 3, EventId = 2, AmountPaid = 200f, CommissionAmount = 20f, OrganizerAmount = 180f, Status = PaymentStatus.SUCCESS, Event = new Event { EventId = 2, Title = "E2", CreatedByUserId = 5 } }
            };
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

            var result = (await CreateService().GetEventWiseEarningsAsync(5)).ToList();
            Assert.Equal(2, result.Count);
            var e1 = result.First(e => e.EventId == 1);
            Assert.Equal(2, e1.TotalTransactions);
        }

        // ── GetOrganizerRefundsPagedAsync ────────────────────────────────

        [Fact]
        public async Task GetOrganizerRefundsPaged_ReturnsPaged()
        {
            var payments = Enumerable.Range(1, 5).Select(i => new Payment
            {
                PaymentId = i,
                Status = PaymentStatus.REFUNDED,
                RefundedAt = DateTime.UtcNow,
                Event = new Event { CreatedByUserId = 5 },
                User = new User()
            }).ToList();
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

            var result = await CreateService().GetOrganizerRefundsPagedAsync(5, 1, 3);
            Assert.Equal(5, result.TotalRecords);
            Assert.Equal(3, result.Data.Count());
        }
    }
}





