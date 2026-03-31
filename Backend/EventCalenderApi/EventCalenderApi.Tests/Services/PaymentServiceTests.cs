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

        // ── CreatePayment — additional branches ──────────────────────────

        [Fact]
        public async Task CreatePayment_EventAlreadyStarted_ThrowsBadRequest()
        {
            var ev = new Event
            {
                EventId = 1,
                IsPaidEvent = true,
                Status = EventStatus.ACTIVE,
                ApprovalStatus = ApprovalStatus.APPROVED,
                TicketPrice = 100f,
                EventDate = DateTime.UtcNow.Date.AddDays(-1), // started yesterday
                StartTime = new TimeSpan(10, 0, 0),
                CreatedByUserId = 5
            };
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
        public async Task CreatePayment_PendingRefund_ThrowsBadRequest()
        {
            // Existing payment is not REFUNDED yet → "Please wait for refund to complete"
            var ev = PaidApprovedEvent();
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            var registrations = new List<EventRegistration>
            {
                new() { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(registrations.BuildMock());

            // Existing payment with PENDING status (not refunded yet)
            var payments = new List<Payment>
            {
                new() { PaymentId = 1, UserId = 1, EventId = 1, Status = PaymentStatus.PENDING }
            };
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 1 }));
        }

        [Fact]
        public async Task CreatePayment_AfterRefund_AllowsRepayment()
        {
            // Previous payment was REFUNDED and user is no longer registered → allow new payment
            var ev = PaidApprovedEvent();
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            var registrations = new List<EventRegistration>
            {
                new() { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(registrations.BuildMock());

            // Previous payment was refunded, user re-registered
            var payments = new List<Payment>
            {
                new() { PaymentId = 1, UserId = 1, EventId = 1, Status = PaymentStatus.REFUNDED }
            };
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

            var created = new Payment { PaymentId = 2, UserId = 1, EventId = 1, AmountPaid = 100f, Status = PaymentStatus.SUCCESS };
            _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>())).ReturnsAsync(created);
            _walletMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 1 });
            Assert.Equal(PaymentStatus.SUCCESS, result.Status);
        }

        [Fact]
        public async Task CreatePayment_NoAdminId_SkipsAdminWalletCredit()
        {
            // ApprovedByUserId is null → admin wallet credit is skipped
            var ev = PaidApprovedEvent();
            ev.ApprovedByUserId = null;
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            var registrations = new List<EventRegistration>
            {
                new() { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(registrations.BuildMock());
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());

            var created = new Payment { PaymentId = 1, UserId = 1, EventId = 1, AmountPaid = 100f, Status = PaymentStatus.SUCCESS };
            _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>())).ReturnsAsync(created);
            _walletMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 1 });
            Assert.Equal(PaymentStatus.SUCCESS, result.Status);
            // Only organizer credit, not admin (adminId = 0)
            _walletMock.Verify(w => w.CreditAsync(5, It.IsAny<float>(), "ORGANIZER_EARNING", It.IsAny<string>()), Times.Once);
            _walletMock.Verify(w => w.CreditAsync(0, It.IsAny<float>(), "COMMISSION", It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetOrganizerEarnings_NoPayments_ReturnsZeros()
        {
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());

            var result = await CreateService().GetOrganizerEarningsAsync(99);
            Assert.Equal(0f, result.TotalRevenue);
            Assert.Equal(0, result.TotalTransactions);
        }

        [Fact]
        public async Task GetEventWiseEarnings_NoPayments_ReturnsEmpty()
        {
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());

            var result = await CreateService().GetEventWiseEarningsAsync(99);
            Assert.Empty(result);
        }

        // ── CreatePayment — event already ended ──────────────────────────

        [Fact]
        public async Task CreatePayment_EventAlreadyEnded_ThrowsBadRequest()
        {
            var ev = new Event
            {
                EventId = 1,
                IsPaidEvent = true,
                Status = EventStatus.ACTIVE,
                ApprovalStatus = ApprovalStatus.APPROVED,
                TicketPrice = 100f,
                EventDate = DateTime.UtcNow.Date.AddDays(-3),
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),   // ended 3 days ago
                CreatedByUserId = 5
            };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            var registrations = new List<EventRegistration>
            {
                new() { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(registrations.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 1 }));
        }

        // ── RefundAsync — sets all fields correctly ──────────────────────

        [Fact]
        public async Task Refund_SetsRefundedAmountAndZerosCommission()
        {
            var payment = new Payment
            {
                PaymentId = 1, UserId = 1, EventId = 1,
                AmountPaid = 150f, CommissionAmount = 15f, OrganizerAmount = 135f,
                Status = PaymentStatus.SUCCESS
            };
            var ev = new Event
            {
                EventId = 1,
                EventDate = DateTime.UtcNow.Date.AddDays(3),
                EndTime = new TimeSpan(12, 0, 0)
            };
            _paymentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(payment);
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, payment)).ReturnsAsync(payment);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            await CreateService().RefundAsync(1);

            Assert.Equal(150f, payment.RefundedAmount);
            Assert.Equal(0f, payment.CommissionAmount);
            Assert.Equal(0f, payment.OrganizerAmount);
            Assert.Equal(PaymentStatus.REFUNDED, payment.Status);
        }

        // ── GetCommissionSummary — no payments ───────────────────────────

        [Fact]
        public async Task GetCommissionSummary_NoPayments_ReturnsZeros()
        {
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());

            var result = await CreateService().GetCommissionSummaryAsync();
            Assert.Equal(0f, result.TotalCommission);
            Assert.Equal(0, result.TotalPayments);
        }

        // ── GetByUser — empty ────────────────────────────────────────────

        [Fact]
        public async Task GetByUser_NoPayments_ReturnsEmpty()
        {
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());
            var result = await CreateService().GetByUserAsync(99);
            Assert.Empty(result);
        }

        // ── CreatePayment — commission calculation ───────────────────────

        [Fact]
        public async Task CreatePayment_CalculatesCommissionCorrectly()
        {
            var ev = PaidApprovedEvent(); // TicketPrice=100, CommissionPercentage=10
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _regRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<EventRegistration> { new() { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED } }.BuildMock());
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());

            Payment? capturedPayment = null;
            _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                .Callback<Payment>(p => capturedPayment = p)
                .ReturnsAsync((Payment p) => p);

            _walletMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            await CreateService().CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 1 });

            Assert.Equal(100f, capturedPayment?.AmountPaid);
            Assert.Equal(10f, capturedPayment?.CommissionAmount);   // 10% of 100
            Assert.Equal(90f, capturedPayment?.OrganizerAmount);    // 100 - 10
        }

        // ── CreatePayment — credits organizer and admin wallets ──────────

        [Fact]
        public async Task CreatePayment_CreditsOrganizerAndAdminWallets()
        {
            var ev = PaidApprovedEvent(); // CreatedByUserId=5, ApprovedByUserId=10
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _regRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<EventRegistration> { new() { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED } }.BuildMock());
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());
            _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>()))
                .ReturnsAsync(new Payment { PaymentId = 1, UserId = 1, EventId = 1, AmountPaid = 100f, Status = PaymentStatus.SUCCESS });
            _walletMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            await CreateService().CreatePaymentAsync(1, new PaymentRequestDTO { EventId = 1 });

            // Organizer gets 90 (100 - 10% commission)
            _walletMock.Verify(w => w.CreditAsync(5, 90f, "ORGANIZER_EARNING", It.IsAny<string>()), Times.Once);
            // Admin gets 10 (10% commission)
            _walletMock.Verify(w => w.CreditAsync(10, 10f, "COMMISSION", It.IsAny<string>()), Times.Once);
        }

        // ── GetByEvent — empty ───────────────────────────────────────────

        [Fact]
        public async Task GetByEvent_NoPayments_ReturnsEmpty()
        {
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());
            var result = await CreateService().GetByEventAsync(99);
            Assert.Empty(result);
        }

        // ── GetAllPayments — empty ───────────────────────────────────────

        [Fact]
        public async Task GetAllPayments_Empty_ReturnsEmpty()
        {
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());
            var result = await CreateService().GetAllPaymentsAsync();
            Assert.Empty(result);
        }
    }
}





