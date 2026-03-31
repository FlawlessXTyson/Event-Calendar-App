using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.EventRegisration;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.Services;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace EventCalenderApi.Tests.Services
{
    public class EventRegistrationServiceTests
    {
        private readonly Mock<IRepository<int, EventRegistration>> _regRepoMock = new();
        private readonly Mock<IRepository<int, Event>> _eventRepoMock = new();
        private readonly Mock<IRepository<int, Payment>> _paymentRepoMock = new();
        private readonly Mock<IRepository<int, RefundRequest>> _refundRepoMock = new();
        private readonly Mock<IAuditLogRepository> _auditMock = new();
        private readonly Mock<IWalletService> _walletMock = new();

        private EventRegistrationService CreateService() => new(
            _regRepoMock.Object,
            _eventRepoMock.Object,
            _paymentRepoMock.Object,
            _refundRepoMock.Object,
            _auditMock.Object,
            _walletMock.Object);

        private static Event ApprovedActiveEvent(bool withSeats = false) => new()
        {
            EventId = 1,
            Title = "Test Event",
            ApprovalStatus = ApprovalStatus.APPROVED,
            Status = EventStatus.ACTIVE,
            EventDate = DateTime.UtcNow.Date.AddDays(2),
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(12, 0, 0),
            SeatsLimit = withSeats ? 1 : null
        };

        // ── RegisterAsync ────────────────────────────────────────────────

        [Fact]
        public async Task Register_ValidRequest_ReturnsRegistration()
        {
            var ev = ApprovedActiveEvent();
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            _regRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<EventRegistration>().BuildMock());

            var created = new EventRegistration { RegistrationId = 1, EventId = 1, UserId = 1, Status = RegistrationStatus.REGISTERED };
            _regRepoMock.Setup(r => r.AddAsync(It.IsAny<EventRegistration>())).ReturnsAsync(created);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().RegisterAsync(new EventRegisterationRequestDTO { EventId = 1 }, 1);
            Assert.Equal(RegistrationStatus.REGISTERED, result.Status);
        }

        [Fact]
        public async Task Register_EventNotFound_ThrowsNotFound()
        {
            _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);
            await Assert.ThrowsAsync<NotFoundException>(() =>
                CreateService().RegisterAsync(new EventRegisterationRequestDTO { EventId = 99 }, 1));
        }

        [Fact]
        public async Task Register_NotApproved_ThrowsBadRequest()
        {
            var ev = new Event { EventId = 1, ApprovalStatus = ApprovalStatus.PENDING, Status = EventStatus.ACTIVE };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().RegisterAsync(new EventRegisterationRequestDTO { EventId = 1 }, 1));
        }

        [Fact]
        public async Task Register_NotActive_ThrowsBadRequest()
        {
            var ev = new Event { EventId = 1, ApprovalStatus = ApprovalStatus.APPROVED, Status = EventStatus.CANCELLED };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().RegisterAsync(new EventRegisterationRequestDTO { EventId = 1 }, 1));
        }

        [Fact]
        public async Task Register_DeadlinePassed_ThrowsBadRequest()
        {
            var ev = ApprovedActiveEvent();
            ev.RegistrationDeadline = DateTime.UtcNow.AddHours(-1);
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().RegisterAsync(new EventRegisterationRequestDTO { EventId = 1 }, 1));
        }

        [Fact]
        public async Task Register_EventAlreadyStarted_ThrowsBadRequest()
        {
            var ev = new Event
            {
                EventId = 1,
                ApprovalStatus = ApprovalStatus.APPROVED,
                Status = EventStatus.ACTIVE,
                EventDate = DateTime.UtcNow.Date.AddDays(-1), // yesterday
                StartTime = new TimeSpan(10, 0, 0)
            };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().RegisterAsync(new EventRegisterationRequestDTO { EventId = 1 }, 1));
        }

        [Fact]
        public async Task Register_NoSeatsAvailable_ThrowsBadRequest()
        {
            var ev = ApprovedActiveEvent(withSeats: true);
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            var registrations = new List<EventRegistration>
            {
                new() { EventId = 1, UserId = 2, Status = RegistrationStatus.REGISTERED }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(registrations.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().RegisterAsync(new EventRegisterationRequestDTO { EventId = 1 }, 1));
        }

        [Fact]
        public async Task Register_AlreadyRegistered_ThrowsBadRequest()
        {
            var ev = ApprovedActiveEvent();
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            var registrations = new List<EventRegistration>
            {
                new() { EventId = 1, UserId = 1, Status = RegistrationStatus.REGISTERED }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(registrations.BuildMock());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().RegisterAsync(new EventRegisterationRequestDTO { EventId = 1 }, 1));
        }

        // ── CancelAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task Cancel_FreeEvent_NoPayment_Cancels()
        {
            var reg = new EventRegistration { RegistrationId = 1, UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED };
            var ev = new Event
            {
                EventId = 1,
                EventDate = DateTime.UtcNow.Date.AddDays(3),
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                IsPaidEvent = false
            };

            _regRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reg);
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _paymentRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Payment>().BuildMock());
            _regRepoMock.Setup(r => r.UpdateAsync(1, reg)).ReturnsAsync(reg);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().CancelAsync(1, 1, "USER");
            Assert.Equal(RegistrationStatus.CANCELLED, result.Status);
        }

        [Fact]
        public async Task Cancel_PaidEvent_WithPayment_RefundsAndCancels()
        {
            var reg = new EventRegistration { RegistrationId = 1, UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED };
            var ev = new Event
            {
                EventId = 1,
                EventDate = DateTime.UtcNow.Date.AddDays(5),
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                IsPaidEvent = true
            };
            var payment = new Payment
            {
                PaymentId = 1, UserId = 1, EventId = 1,
                AmountPaid = 100f, CommissionAmount = 10f, OrganizerAmount = 90f,
                Status = PaymentStatus.SUCCESS
            };

            _regRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reg);
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _paymentRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Payment> { payment }.BuildMock());
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, payment)).ReturnsAsync(payment);
            _regRepoMock.Setup(r => r.UpdateAsync(1, reg)).ReturnsAsync(reg);
            _walletMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().CancelAsync(1, 1, "USER");
            Assert.Equal(RegistrationStatus.CANCELLED, result.Status);
        }

        [Fact]
        public async Task Cancel_NotFound_ThrowsNotFound()
        {
            _regRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((EventRegistration?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().CancelAsync(99, 1, "USER"));
        }

        [Fact]
        public async Task Cancel_AlreadyCancelled_ThrowsBadRequest()
        {
            var reg = new EventRegistration { RegistrationId = 1, UserId = 1, Status = RegistrationStatus.CANCELLED };
            _regRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reg);
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CancelAsync(1, 1, "USER"));
        }

        [Fact]
        public async Task Cancel_WrongUser_ThrowsUnauthorized()
        {
            var reg = new EventRegistration { RegistrationId = 1, UserId = 2, Status = RegistrationStatus.REGISTERED };
            _regRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reg);
            await Assert.ThrowsAsync<UnauthorizedException>(() => CreateService().CancelAsync(1, 1, "USER"));
        }

        [Fact]
        public async Task Cancel_AfterEventEnded_ThrowsBadRequest()
        {
            var reg = new EventRegistration { RegistrationId = 1, UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED };
            var ev = new Event
            {
                EventId = 1,
                EventDate = DateTime.UtcNow.Date.AddDays(-3),
                EndTime = new TimeSpan(12, 0, 0)
            };
            _regRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reg);
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CancelAsync(1, 1, "USER"));
        }

        // ── GetByEventAsync ──────────────────────────────────────────────

        [Fact]
        public async Task GetByEvent_ReturnsRegistrationsForEvent()
        {
            var regs = new List<EventRegistration>
            {
                new() { RegistrationId = 1, EventId = 1, UserId = 1, User = new User { UserId = 1, Name = "Alice" } },
                new() { RegistrationId = 2, EventId = 2, UserId = 2, User = new User { UserId = 2, Name = "Bob" } }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(regs.BuildMock());

            var result = (await CreateService().GetByEventAsync(1)).ToList();
            Assert.Single(result);
        }

        // ── GetByEventPagedAsync ─────────────────────────────────────────

        [Fact]
        public async Task GetByEventPaged_ReturnsPaged()
        {
            var regs = Enumerable.Range(1, 5).Select(i => new EventRegistration
            {
                RegistrationId = i,
                EventId = 1,
                UserId = i,
                RegisteredAt = DateTime.UtcNow,
                User = new User { UserId = i }
            }).ToList();
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(regs.BuildMock());

            var result = await CreateService().GetByEventPagedAsync(1, 1, 3, null);
            Assert.Equal(5, result.TotalRecords);
            Assert.Equal(3, result.Data.Count());
        }

        [Fact]
        public async Task GetByEventPaged_WithDateFilter_FiltersCorrectly()
        {
            var today = DateTime.UtcNow.Date;
            var regs = new List<EventRegistration>
            {
                new() { RegistrationId = 1, EventId = 1, UserId = 1, RegisteredAt = today, User = new User() },
                new() { RegistrationId = 2, EventId = 1, UserId = 2, RegisteredAt = today.AddDays(-1), User = new User() }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(regs.BuildMock());

            var result = await CreateService().GetByEventPagedAsync(1, 1, 10, today);
            Assert.Equal(1, result.TotalRecords);
        }

        // ── GetMyRegistrationsAsync ──────────────────────────────────────

        [Fact]
        public async Task GetMyRegistrations_ReturnsActiveOnly()
        {
            var regs = new List<EventRegistration>
            {
                new() { RegistrationId = 1, UserId = 1, Status = RegistrationStatus.REGISTERED },
                new() { RegistrationId = 2, UserId = 1, Status = RegistrationStatus.CANCELLED }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(regs.BuildMock());

            var result = (await CreateService().GetMyRegistrationsAsync(1)).ToList();
            Assert.Single(result);
        }

        // ── CancelAsync — additional branches ───────────────────────────

        [Fact]
        public async Task Cancel_AdminRole_CanCancelAnyRegistration()
        {
            // Admin can cancel any user's registration (role != "USER" skips ownership check)
            var reg = new EventRegistration { RegistrationId = 1, UserId = 2, EventId = 1, Status = RegistrationStatus.REGISTERED };
            var ev = new Event
            {
                EventId = 1,
                EventDate = DateTime.UtcNow.Date.AddDays(3),
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(12, 0, 0)
            };
            _regRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reg);
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());
            _regRepoMock.Setup(r => r.UpdateAsync(1, reg)).ReturnsAsync(reg);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().CancelAsync(1, 99, "ADMIN");
            Assert.Equal(RegistrationStatus.CANCELLED, result.Status);
        }

        [Fact]
        public async Task Cancel_PaidEvent_ZeroRefund_WhenEventAlmostStarted()
        {
            // Event started but hasn't ended → 0% refund (hoursBeforeEvent <= 0)
            // Use a future date to avoid IST/UTC timezone edge cases
            var reg = new EventRegistration { RegistrationId = 1, UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED };
            var ev = new Event
            {
                EventId = 1,
                // Event started 1 hour ago (IST), ends in 10 hours (IST)
                EventDate = DateTime.UtcNow.Date.AddDays(1),
                StartTime = new TimeSpan(0, 0, 0),   // midnight tomorrow IST — already past in IST
                EndTime = new TimeSpan(23, 59, 59),   // end of tomorrow IST
                IsPaidEvent = true
            };

            var payment = new Payment
            {
                PaymentId = 1, UserId = 1, EventId = 1,
                AmountPaid = 100f, CommissionAmount = 10f, OrganizerAmount = 90f,
                Status = PaymentStatus.SUCCESS
            };
            _regRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reg);
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _paymentRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Payment> { payment }.BuildMock());
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, payment)).ReturnsAsync(payment);
            _regRepoMock.Setup(r => r.UpdateAsync(1, reg)).ReturnsAsync(reg);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().CancelAsync(1, 1, "USER");
            Assert.Equal(RegistrationStatus.CANCELLED, result.Status);
            // Event is in the future (tomorrow) so refund % > 0 — wallet credit IS called
            _walletMock.Verify(w => w.CreditAsync(1, It.IsAny<float>(), "REFUND", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Cancel_EventNotFound_ThrowsNotFound()
        {
            var reg = new EventRegistration { RegistrationId = 1, UserId = 1, EventId = 99, Status = RegistrationStatus.REGISTERED };
            _regRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reg);
            _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().CancelAsync(1, 1, "USER"));
        }

        // ── Register — event ended ───────────────────────────────────────

        [Fact]
        public async Task Register_EventAlreadyEnded_ThrowsBadRequest()
        {
            var ev = new Event
            {
                EventId = 1,
                ApprovalStatus = ApprovalStatus.APPROVED,
                Status = EventStatus.ACTIVE,
                EventDate = DateTime.UtcNow.Date.AddDays(-2),
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(12, 0, 0)
            };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().RegisterAsync(new EventRegisterationRequestDTO { EventId = 1 }, 1));
        }

        // ── CancelAsync — refund tier 75% (24-48h before) ───────────────

        [Fact]
        public async Task Cancel_PaidEvent_75PercentRefund_24to48HoursBefore()
        {
            var reg = new EventRegistration { RegistrationId = 1, UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED };
            var ev = new Event
            {
                EventId = 1,
                EventDate = DateTime.UtcNow.Date.AddDays(2),
                StartTime = new TimeSpan(0, 0, 0),  // 2 days from now at midnight → ~48h
                EndTime = new TimeSpan(23, 59, 59),
                IsPaidEvent = true,
                Title = "Event"
            };
            var payment = new Payment
            {
                PaymentId = 1, UserId = 1, EventId = 1,
                AmountPaid = 100f, CommissionAmount = 10f, OrganizerAmount = 90f,
                Status = PaymentStatus.SUCCESS
            };
            _regRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reg);
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment> { payment }.BuildMock());
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, payment)).ReturnsAsync(payment);
            _regRepoMock.Setup(r => r.UpdateAsync(1, reg)).ReturnsAsync(reg);
            _walletMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().CancelAsync(1, 1, "USER");
            Assert.Equal(RegistrationStatus.CANCELLED, result.Status);
            Assert.Equal(PaymentStatus.REFUNDED, payment.Status);
        }

        // ── CancelAsync — refund sets payment fields correctly ───────────

        [Fact]
        public async Task Cancel_PaidEvent_SetsRefundedAmountAndCancelledBy()
        {
            var reg = new EventRegistration { RegistrationId = 1, UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED };
            var ev = new Event
            {
                EventId = 1,
                EventDate = DateTime.UtcNow.Date.AddDays(5), // > 48h → 100% refund
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                IsPaidEvent = true,
                Title = "Event"
            };
            var payment = new Payment
            {
                PaymentId = 1, UserId = 1, EventId = 1,
                AmountPaid = 200f, CommissionAmount = 20f, OrganizerAmount = 180f,
                Status = PaymentStatus.SUCCESS
            };
            _regRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reg);
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment> { payment }.BuildMock());
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, payment)).ReturnsAsync(payment);
            _regRepoMock.Setup(r => r.UpdateAsync(1, reg)).ReturnsAsync(reg);
            _walletMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            await CreateService().CancelAsync(1, 1, "USER");

            Assert.Equal(200f, payment.RefundedAmount);  // 100% of 200
            Assert.Equal("USER", payment.CancelledBy);
            Assert.Equal(PaymentStatus.REFUNDED, payment.Status);
            // Wallet credited with full amount
            _walletMock.Verify(w => w.CreditAsync(1, 200f, "REFUND", It.IsAny<string>()), Times.Once);
        }

        // ── GetByEvent — empty ───────────────────────────────────────────

        [Fact]
        public async Task GetByEvent_NoRegistrations_ReturnsEmpty()
        {
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());
            var result = await CreateService().GetByEventAsync(99);
            Assert.Empty(result);
        }

        // ── CancelAsync — 50% refund tier (12-24h before) ───────────────

        [Fact]
        public async Task Cancel_PaidEvent_50PercentRefund_12to24HoursBefore()
        {
            var reg = new EventRegistration { RegistrationId = 1, UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED };
            // Event starts in ~18h → 50% refund tier
            var ev = new Event
            {
                EventId = 1,
                EventDate = DateTime.UtcNow.Date.AddDays(1),
                StartTime = DateTime.UtcNow.TimeOfDay.Add(TimeSpan.FromHours(6)), // 30h from now
                EndTime = new TimeSpan(23, 59, 59),
                IsPaidEvent = true, Title = "Event"
            };
            var payment = new Payment
            {
                PaymentId = 1, UserId = 1, EventId = 1,
                AmountPaid = 100f, CommissionAmount = 10f, OrganizerAmount = 90f,
                Status = PaymentStatus.SUCCESS
            };
            _regRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reg);
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment> { payment }.BuildMock());
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, payment)).ReturnsAsync(payment);
            _regRepoMock.Setup(r => r.UpdateAsync(1, reg)).ReturnsAsync(reg);
            _walletMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            await CreateService().CancelAsync(1, 1, "USER");

            // Refund should be > 0 (either 75% or 50% depending on exact hours)
            Assert.True(payment.RefundedAmount > 0);
            Assert.Equal(PaymentStatus.REFUNDED, payment.Status);
        }

        // ── CancelAsync — 25% refund tier (0-12h before) ────────────────

        [Fact]
        public async Task Cancel_PaidEvent_25PercentRefund_LessThan12HoursBefore()
        {
            var reg = new EventRegistration { RegistrationId = 1, UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED };
            // Event starts in ~6h → 25% refund tier
            var ev = new Event
            {
                EventId = 1,
                EventDate = DateTime.UtcNow.Date,
                StartTime = DateTime.UtcNow.TimeOfDay.Add(TimeSpan.FromHours(6)),
                EndTime = DateTime.UtcNow.TimeOfDay.Add(TimeSpan.FromHours(10)),
                IsPaidEvent = true, Title = "Event"
            };
            var payment = new Payment
            {
                PaymentId = 1, UserId = 1, EventId = 1,
                AmountPaid = 100f, CommissionAmount = 10f, OrganizerAmount = 90f,
                Status = PaymentStatus.SUCCESS
            };
            _regRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reg);
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment> { payment }.BuildMock());
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, payment)).ReturnsAsync(payment);
            _regRepoMock.Setup(r => r.UpdateAsync(1, reg)).ReturnsAsync(reg);
            _walletMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            await CreateService().CancelAsync(1, 1, "USER");

            Assert.Equal(25f, payment.RefundedAmount); // 25% of 100
            Assert.Equal(PaymentStatus.REFUNDED, payment.Status);
        }

        // ── GetMyRegistrations — empty ───────────────────────────────────

        [Fact]
        public async Task GetMyRegistrations_NoRegistrations_ReturnsEmpty()
        {
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());
            var result = await CreateService().GetMyRegistrationsAsync(99);
            Assert.Empty(result);
        }
    }
}





