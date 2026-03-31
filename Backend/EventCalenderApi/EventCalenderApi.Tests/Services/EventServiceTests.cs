using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Event;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.Services;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace EventCalenderApi.Tests.Services
{
    public class EventServiceTests
    {
        private readonly Mock<IRepository<int, Event>> _eventRepoMock = new();
        private readonly Mock<IRepository<int, User>> _userRepoMock = new();
        private readonly Mock<IRepository<int, EventRegistration>> _regRepoMock = new();
        private readonly Mock<IRepository<int, Payment>> _paymentRepoMock = new();
        private readonly Mock<IAuditLogRepository> _auditMock = new();
        private readonly Mock<IWalletService> _walletMock = new();

        private EventService CreateService() => new(
            _eventRepoMock.Object,
            _userRepoMock.Object,
            _regRepoMock.Object,
            _paymentRepoMock.Object,
            _auditMock.Object,
            _walletMock.Object);

        private static CreateEventRequestDTO FutureEventDto(bool isPaid = false) => new()
        {
            Title = "Test Event",
            Description = "Desc",
            EventDate = DateTime.UtcNow.Date.AddDays(3),
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(12, 0, 0),
            Location = "Hall A",
            CreatedByUserId = 1,
            IsPaidEvent = isPaid,
            TicketPrice = isPaid ? 100f : 0f
        };

        // ── CreateEventAsync ─────────────────────────────────────────────

        [Fact]
        public async Task CreateEvent_ValidRequest_ReturnsEvent()
        {
            var dto = FutureEventDto();
            _eventRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Event>().BuildMock());
            _eventRepoMock.Setup(r => r.AddAsync(It.IsAny<Event>()))
                .ReturnsAsync(new Event { EventId = 1, Title = "Test Event" });
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().CreateEventAsync(dto);
            Assert.Equal("Test Event", result.Title);
        }

        [Fact]
        public async Task CreateEvent_PastDate_ThrowsBadRequest()
        {
            var dto = FutureEventDto();
            dto.EventDate = DateTime.UtcNow.Date.AddDays(-1);
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateEventAsync(dto));
        }

        [Fact]
        public async Task CreateEvent_EndTimeBeforeStartTime_ThrowsBadRequest()
        {
            var dto = FutureEventDto();
            dto.StartTime = new TimeSpan(12, 0, 0);
            dto.EndTime = new TimeSpan(10, 0, 0);
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateEventAsync(dto));
        }

        [Fact]
        public async Task CreateEvent_EndDateBeforeStartDate_ThrowsBadRequest()
        {
            var dto = FutureEventDto();
            dto.EventEndDate = dto.EventDate.AddDays(-1);
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateEventAsync(dto));
        }

        [Fact]
        public async Task CreateEvent_PastRegistrationDeadline_ThrowsBadRequest()
        {
            var dto = FutureEventDto();
            dto.RegistrationDeadline = DateTime.UtcNow.AddHours(-1);
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateEventAsync(dto));
        }

        [Fact]
        public async Task CreateEvent_DeadlineAfterEventStart_ThrowsBadRequest()
        {
            var dto = FutureEventDto();
            dto.RegistrationDeadline = dto.EventDate.Add(dto.StartTime!.Value).AddHours(1);
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateEventAsync(dto));
        }

        [Fact]
        public async Task CreateEvent_DuplicateEvent_ThrowsBadRequest()
        {
            var dto = FutureEventDto();
            var existing = new List<Event>
            {
                new()
                {
                    Title = dto.Title,
                    EventDate = dto.EventDate,
                    StartTime = dto.StartTime,
                    CreatedByUserId = dto.CreatedByUserId,
                    Status = EventStatus.ACTIVE
                }
            };
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(existing.BuildMock());
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateEventAsync(dto));
        }

        [Fact]
        public async Task CreateEvent_PaidWithZeroPrice_ThrowsBadRequest()
        {
            var dto = FutureEventDto(isPaid: true);
            dto.TicketPrice = 0;
            _eventRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Event>().BuildMock());
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateEventAsync(dto));
        }

        // ── GetAllAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetAll_ReturnsApprovedActiveEvents()
        {
            var events = new List<Event>
            {
                new() { EventId = 1, Title = "E1", Status = EventStatus.ACTIVE, ApprovalStatus = ApprovalStatus.APPROVED, CreatedBy = new User() },
                new() { EventId = 2, Title = "E2", Status = EventStatus.CANCELLED, ApprovalStatus = ApprovalStatus.APPROVED, CreatedBy = new User() }
            };
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());

            var result = (await CreateService().GetAllAsync()).ToList();
            Assert.Single(result);
            Assert.Equal("E1", result[0].Title);
        }

        // ── GetByIdAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task GetById_Found_ReturnsEvent()
        {
            var ev = new Event { EventId = 1, Title = "E1", IsPaidEvent = false };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());

            var result = await CreateService().GetByIdAsync(1);
            Assert.Equal("E1", result.Title);
        }

        [Fact]
        public async Task GetById_NotFound_ThrowsNotFound()
        {
            _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetByIdAsync(99));
        }

        // ── DeleteAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task Delete_Found_ReturnsDeleted()
        {
            var ev = new Event { EventId = 1, Title = "E1" };
            _eventRepoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(ev);

            var result = await CreateService().DeleteAsync(1);
            Assert.Equal("E1", result.Title);
        }

        [Fact]
        public async Task Delete_NotFound_ThrowsNotFound()
        {
            _eventRepoMock.Setup(r => r.DeleteAsync(99)).ReturnsAsync((Event?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().DeleteAsync(99));
        }

        // ── ApproveAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task Approve_Success_SetsApproved()
        {
            var ev = new Event { EventId = 1, ApprovalStatus = ApprovalStatus.PENDING };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _eventRepoMock.Setup(r => r.UpdateAsync(1, ev)).ReturnsAsync(ev);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().ApproveAsync(1, 99);
            Assert.Equal(ApprovalStatus.APPROVED, result.ApprovalStatus);
        }

        [Fact]
        public async Task Approve_NotFound_ThrowsNotFound()
        {
            _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().ApproveAsync(99, 1));
        }

        // ── RejectAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task Reject_Success_SetsRejected()
        {
            var ev = new Event { EventId = 1, ApprovalStatus = ApprovalStatus.PENDING };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _eventRepoMock.Setup(r => r.UpdateAsync(1, ev)).ReturnsAsync(ev);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().RejectAsync(1, 99);
            Assert.Equal(ApprovalStatus.REJECTED, result.ApprovalStatus);
        }

        [Fact]
        public async Task Reject_NotFound_ThrowsNotFound()
        {
            _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().RejectAsync(99, 1));
        }
        // -- SearchAsync --------------------------------------------------

        [Fact]
        public async Task Search_ReturnsMatchingEvents()
        {
            var events = new List<Event>
            {
                new() { EventId = 1, Title = "Music Festival" },
                new() { EventId = 2, Title = "Tech Conference" }
            };
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

            var result = (await CreateService().SearchAsync("Music")).ToList();
            Assert.Single(result);
            Assert.Equal("Music Festival", result[0].Title);
        }

        [Fact]
        public async Task Search_NoMatch_ReturnsEmpty()
        {
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Event>().BuildMock());
            var result = await CreateService().SearchAsync("xyz");
            Assert.Empty(result);
        }

        // -- GetByDateRangeAsync ------------------------------------------

        [Fact]
        public async Task GetByDateRange_ReturnsEventsInRange()
        {
            var start = DateTime.UtcNow.Date;
            var end = start.AddDays(7);
            var events = new List<Event>
            {
                new() { EventId = 1, EventDate = start.AddDays(2) },
                new() { EventId = 2, EventDate = start.AddDays(10) }
            };
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

            var result = (await CreateService().GetByDateRangeAsync(start, end)).ToList();
            Assert.Single(result);
        }

        // -- GetPagedAsync ------------------------------------------------

        [Fact]
        public async Task GetPaged_ReturnsPaged()
        {
            var events = Enumerable.Range(1, 10).Select(i => new Event { EventId = i, Title = $"E{i}" }).ToList();
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

            var result = await CreateService().GetPagedAsync(1, 5);
            Assert.Equal(10, result.TotalRecords);
            Assert.Equal(5, result.Data.Count());
        }

        // -- GetMyEventsAsync ---------------------------------------------

        [Fact]
        public async Task GetMyEvents_ReturnsOrganizerEvents()
        {
            var events = new List<Event>
            {
                new() { EventId = 1, CreatedByUserId = 1 },
                new() { EventId = 2, CreatedByUserId = 2 }
            };
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

            var result = (await CreateService().GetMyEventsAsync(1)).ToList();
            Assert.Single(result);
        }

        // -- GetMyEventsPagedAsync ----------------------------------------

        [Fact]
        public async Task GetMyEventsPaged_ReturnsPaged()
        {
            var events = Enumerable.Range(1, 5).Select(i => new Event
            {
                EventId = i, CreatedByUserId = 1,
                EventDate = DateTime.UtcNow.Date.AddDays(i),
                StartTime = new TimeSpan(10, 0, 0)
            }).ToList();
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

            var result = await CreateService().GetMyEventsPagedAsync(1, 1, 3, null);
            Assert.Equal(5, result.TotalRecords);
            Assert.Equal(3, result.Data.Count());
        }

        [Fact]
        public async Task GetMyEventsPaged_WithDateFilter_FiltersCorrectly()
        {
            var today = DateTime.UtcNow.Date;
            var events = new List<Event>
            {
                new() { EventId = 1, CreatedByUserId = 1, EventDate = today, StartTime = new TimeSpan(10, 0, 0) },
                new() { EventId = 2, CreatedByUserId = 1, EventDate = today.AddDays(1), StartTime = new TimeSpan(10, 0, 0) }
            };
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

            var result = await CreateService().GetMyEventsPagedAsync(1, 1, 10, today);
            Assert.Equal(1, result.TotalRecords);
        }

        // -- GetRegisteredEventsAsync -------------------------------------

        [Fact]
        public async Task GetRegisteredEvents_ReturnsUserRegisteredEvents()
        {
            var ev = new Event { EventId = 1, Title = "E1" };
            var regs = new List<EventRegistration>
            {
                new() { UserId = 1, EventId = 1, Status = RegistrationStatus.REGISTERED, Event = ev },
                new() { UserId = 1, EventId = 2, Status = RegistrationStatus.CANCELLED, Event = new Event { EventId = 2 } }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(regs.BuildMock());

            var result = (await CreateService().GetRegisteredEventsAsync(1)).ToList();
            Assert.Single(result);
        }

        // -- GetRefundSummaryAsync ----------------------------------------

        [Fact]
        public async Task GetRefundSummary_ReturnsCorrectTotals()
        {
            var payments = new List<Payment>
            {
                new() { EventId = 1, Status = PaymentStatus.REFUNDED, RefundedAmount = 100f },
                new() { EventId = 1, Status = PaymentStatus.REFUNDED, RefundedAmount = 50f },
                new() { EventId = 1, Status = PaymentStatus.SUCCESS }
            };
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

            var result = await CreateService().GetRefundSummaryAsync(1);
            Assert.Equal(2, result.TotalUsersRefunded);
            Assert.Equal(150f, result.TotalRefundAmount);
        }

        // -- GetPendingEventsAsync ----------------------------------------

        [Fact]
        public async Task GetPendingEvents_ReturnsUpcomingPending()
        {
            var events = new List<Event>
            {
                new() { EventId = 1, ApprovalStatus = ApprovalStatus.PENDING, EventDate = DateTime.UtcNow.Date.AddDays(2), StartTime = new TimeSpan(10, 0, 0), CreatedBy = new User() },
                new() { EventId = 2, ApprovalStatus = ApprovalStatus.PENDING, EventDate = DateTime.UtcNow.Date.AddDays(-1), StartTime = new TimeSpan(10, 0, 0), CreatedBy = new User() }
            };
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

            var result = (await CreateService().GetPendingEventsAsync()).ToList();
            Assert.Single(result);
        }

        // -- GetRejectedEventsAsync ---------------------------------------

        [Fact]
        public async Task GetRejectedEvents_ReturnsRejected()
        {
            var events = new List<Event>
            {
                new() { EventId = 1, ApprovalStatus = ApprovalStatus.REJECTED, EventDate = DateTime.UtcNow.Date.AddDays(1), CreatedBy = new User() },
                new() { EventId = 2, ApprovalStatus = ApprovalStatus.APPROVED, EventDate = DateTime.UtcNow.Date.AddDays(1), CreatedBy = new User() }
            };
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

            var result = (await CreateService().GetRejectedEventsAsync()).ToList();
            Assert.Single(result);
        }

        // -- GetApprovedEventsAsync ---------------------------------------

        [Fact]
        public async Task GetApprovedEvents_ReturnsApproved()
        {
            var events = new List<Event>
            {
                new() { EventId = 1, ApprovalStatus = ApprovalStatus.APPROVED, EventDate = DateTime.UtcNow.Date.AddDays(1), CreatedBy = new User() },
                new() { EventId = 2, ApprovalStatus = ApprovalStatus.PENDING, EventDate = DateTime.UtcNow.Date.AddDays(1), CreatedBy = new User() }
            };
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

            var result = (await CreateService().GetApprovedEventsAsync()).ToList();
            Assert.Single(result);
        }

        // -- GetExpiredEventsAsync ----------------------------------------

        [Fact]
        public async Task GetExpiredEvents_ReturnsPastEvents()
        {
            var events = new List<Event>
            {
                new() { EventId = 1, EventDate = DateTime.UtcNow.Date.AddDays(-5), CreatedBy = new User() },
                new() { EventId = 2, EventDate = DateTime.UtcNow.Date.AddDays(5), CreatedBy = new User() }
            };
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());

            var result = (await CreateService().GetExpiredEventsAsync()).ToList();
            Assert.Single(result);
        }

        // -- GetCancelledEventsPagedAsync ---------------------------------

        [Fact]
        public async Task GetCancelledEventsPaged_ReturnsCancelled()
        {
            var events = Enumerable.Range(1, 4).Select(i => new Event
            {
                EventId = i, Status = EventStatus.CANCELLED,
                EventDate = DateTime.UtcNow.Date.AddDays(-i), CreatedBy = new User()
            }).ToList();
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

            var result = await CreateService().GetCancelledEventsPagedAsync(1, 3);
            Assert.Equal(4, result.TotalRecords);
            Assert.Equal(3, result.Data.Count());
        }

        // -- GetAllEventsPagedAsync ---------------------------------------

        [Fact]
        public async Task GetAllEventsPaged_WithSearch_FiltersCorrectly()
        {
            var events = new List<Event>
            {
                new() { EventId = 1, Title = "Music Fest", Location = "Hall A", EventDate = DateTime.UtcNow.Date, CreatedBy = new User() },
                new() { EventId = 2, Title = "Tech Talk", Location = "Room B", EventDate = DateTime.UtcNow.Date, CreatedBy = new User() }
            };
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

            var result = await CreateService().GetAllEventsPagedAsync(1, 10, "Music");
            Assert.Equal(1, result.TotalRecords);
        }

        [Fact]
        public async Task GetAllEventsPaged_NoSearch_ReturnsAll()
        {
            var events = Enumerable.Range(1, 5).Select(i => new Event
            {
                EventId = i, Title = $"E{i}", EventDate = DateTime.UtcNow.Date, CreatedBy = new User()
            }).ToList();
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

            var result = await CreateService().GetAllEventsPagedAsync(1, 10, null);
            Assert.Equal(5, result.TotalRecords);
        }

        // -- CancelEventAsync ---------------------------------------------

        [Fact]
        public async Task CancelEvent_NotFound_ThrowsNotFound()
        {
            _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().CancelEventAsync(99, 1, "ADMIN"));
        }

        [Fact]
        public async Task CancelEvent_Organizer_WrongUser_ThrowsUnauthorized()
        {
            var ev = new Event
            {
                EventId = 1, CreatedByUserId = 2,
                EventDate = DateTime.UtcNow.Date.AddDays(5),
                StartTime = new TimeSpan(10, 0, 0)
            };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                CreateService().CancelEventAsync(1, 1, "ORGANIZER"));
        }

        [Fact]
        public async Task CancelEvent_Admin_FreeEvent_CancelsWithNoRefunds()
        {
            var ev = new Event
            {
                EventId = 1, CreatedByUserId = 5, IsPaidEvent = false, Status = EventStatus.ACTIVE,
                EventDate = DateTime.UtcNow.Date.AddDays(5), StartTime = new TimeSpan(10, 0, 0)
            };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());
            _eventRepoMock.Setup(r => r.UpdateAsync(1, ev)).ReturnsAsync(ev);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            await CreateService().CancelEventAsync(1, 99, "ADMIN");
            Assert.Equal(EventStatus.CANCELLED, ev.Status);
        }

        [Fact]
        public async Task CancelEvent_Admin_PaidEvent_RefundsPayments()
        {
            var ev = new Event
            {
                EventId = 1, Title = "Paid Event", CreatedByUserId = 5, ApprovedByUserId = 10,
                IsPaidEvent = true, TicketPrice = 100f, Status = EventStatus.ACTIVE,
                EventDate = DateTime.UtcNow.Date.AddDays(5), StartTime = new TimeSpan(10, 0, 0)
            };
            var payment = new Payment
            {
                PaymentId = 1, UserId = 1, EventId = 1,
                AmountPaid = 100f, CommissionAmount = 10f, OrganizerAmount = 90f,
                Status = PaymentStatus.SUCCESS
            };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _paymentRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Payment> { payment }.BuildMock());
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, payment)).ReturnsAsync(payment);
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());
            _eventRepoMock.Setup(r => r.UpdateAsync(1, ev)).ReturnsAsync(ev);
            _walletMock.Setup(w => w.DebitAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _walletMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().CancelEventAsync(1, 99, "ADMIN");
            Assert.Equal(EventStatus.CANCELLED, ev.Status);
        }

        // -- Paged variants -----------------------------------------------

        [Fact]
        public async Task GetPendingEventsPaged_ReturnsPaged()
        {
            var events = Enumerable.Range(1, 5).Select(i => new Event
            {
                EventId = i, ApprovalStatus = ApprovalStatus.PENDING,
                EventDate = DateTime.UtcNow.Date.AddDays(i + 1),
                StartTime = new TimeSpan(10, 0, 0), CreatedBy = new User()
            }).ToList();
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

            var result = await CreateService().GetPendingEventsPagedAsync(1, 3);
            Assert.Equal(5, result.TotalRecords);
            Assert.Equal(3, result.Data.Count());
        }

        [Fact]
        public async Task GetRejectedEventsPaged_ReturnsPaged()
        {
            var events = Enumerable.Range(1, 4).Select(i => new Event
            {
                EventId = i, ApprovalStatus = ApprovalStatus.REJECTED,
                EventDate = DateTime.UtcNow.Date.AddDays(i), CreatedBy = new User()
            }).ToList();
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

            var result = await CreateService().GetRejectedEventsPagedAsync(1, 3);
            Assert.Equal(4, result.TotalRecords);
        }

        [Fact]
        public async Task GetApprovedEventsPaged_ReturnsPaged()
        {
            var events = Enumerable.Range(1, 6).Select(i => new Event
            {
                EventId = i, ApprovalStatus = ApprovalStatus.APPROVED,
                EventDate = DateTime.UtcNow.Date.AddDays(i), CreatedBy = new User()
            }).ToList();
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

            var result = await CreateService().GetApprovedEventsPagedAsync(1, 4);
            Assert.Equal(6, result.TotalRecords);
            Assert.Equal(4, result.Data.Count());
        }

        [Fact]
        public async Task GetExpiredEventsPaged_ReturnsPaged()
        {
            var events = Enumerable.Range(1, 5).Select(i => new Event
            {
                EventId = i, EventDate = DateTime.UtcNow.Date.AddDays(-i), CreatedBy = new User()
            }).ToList();
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());

            var result = await CreateService().GetExpiredEventsPagedAsync(1, 3);
            Assert.Equal(5, result.TotalRecords);
            Assert.Equal(3, result.Data.Count());
        }

        // ── CancelEventAsync — additional branches ───────────────────────

        [Fact]
        public async Task CancelEvent_AlreadyStarted_ThrowsBadRequest()
        {
            var ev = new Event
            {
                EventId = 1,
                CreatedByUserId = 1,
                EventDate = DateTime.UtcNow.Date.AddDays(-1), // started yesterday
                StartTime = new TimeSpan(10, 0, 0)
            };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().CancelEventAsync(1, 1, "ORGANIZER"));
        }

        [Fact]
        public async Task CancelEvent_Organizer_OwnEvent_NoPayments_Cancels()
        {
            var ev = new Event
            {
                EventId = 1,
                CreatedByUserId = 1,
                IsPaidEvent = false,
                Status = EventStatus.ACTIVE,
                EventDate = DateTime.UtcNow.Date.AddDays(5),
                StartTime = new TimeSpan(10, 0, 0)
            };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());
            _eventRepoMock.Setup(r => r.UpdateAsync(1, ev)).ReturnsAsync(ev);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            await CreateService().CancelEventAsync(1, 1, "ORGANIZER");
            Assert.Equal(EventStatus.CANCELLED, ev.Status);
        }

        [Fact]
        public async Task CancelEvent_Organizer_PaidEvent_EarlyCancel_RefundsWithNoAdminDebit()
        {
            // > 48h before start → no penalty, admin commission refunded, org earnings refunded
            var ev = new Event
            {
                EventId = 1,
                Title = "Paid",
                CreatedByUserId = 1,
                ApprovedByUserId = 10,
                IsPaidEvent = true,
                TicketPrice = 100f,
                Status = EventStatus.ACTIVE,
                EventDate = DateTime.UtcNow.Date.AddDays(5),
                StartTime = new TimeSpan(10, 0, 0)
            };
            var payment = new Payment
            {
                PaymentId = 1, UserId = 2, EventId = 1,
                AmountPaid = 100f, CommissionAmount = 10f, OrganizerAmount = 90f,
                Status = PaymentStatus.SUCCESS
            };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _paymentRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Payment> { payment }.BuildMock());
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, payment)).ReturnsAsync(payment);
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());
            _eventRepoMock.Setup(r => r.UpdateAsync(1, ev)).ReturnsAsync(ev);
            _walletMock.Setup(w => w.DebitAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _walletMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            await CreateService().CancelEventAsync(1, 1, "ORGANIZER");
            Assert.Equal(EventStatus.CANCELLED, ev.Status);
            Assert.Equal(PaymentStatus.REFUNDED, payment.Status);
        }

        [Fact]
        public async Task CancelEvent_Admin_LateCancel_PaidEvent_CreditsOrganizerCompensation()
        {
            // < 48h before start → admin cancels → organizer gets 50%/ticket compensation
            var ev = new Event
            {
                EventId = 1,
                Title = "Late Cancel",
                CreatedByUserId = 5,
                ApprovedByUserId = 10,
                IsPaidEvent = true,
                TicketPrice = 100f,
                Status = EventStatus.ACTIVE,
                EventDate = DateTime.UtcNow.Date,
                StartTime = DateTime.UtcNow.TimeOfDay.Add(TimeSpan.FromHours(24)) // 24h from now → < 48h
            };
            var payment = new Payment
            {
                PaymentId = 1, UserId = 2, EventId = 1,
                AmountPaid = 100f, CommissionAmount = 10f, OrganizerAmount = 90f,
                Status = PaymentStatus.SUCCESS
            };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _paymentRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Payment> { payment }.BuildMock());
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, payment)).ReturnsAsync(payment);
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());
            _eventRepoMock.Setup(r => r.UpdateAsync(1, ev)).ReturnsAsync(ev);
            _walletMock.Setup(w => w.DebitAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _walletMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            await CreateService().CancelEventAsync(1, 99, "ADMIN");
            Assert.Equal(EventStatus.CANCELLED, ev.Status);
        }

        [Fact]
        public async Task CancelEvent_Admin_CancelsRegistrationsAlongWithEvent()
        {
            var ev = new Event
            {
                EventId = 1, CreatedByUserId = 5, IsPaidEvent = false, Status = EventStatus.ACTIVE,
                EventDate = DateTime.UtcNow.Date.AddDays(5), StartTime = new TimeSpan(10, 0, 0)
            };
            var reg = new EventRegistration { RegistrationId = 1, EventId = 1, UserId = 2, Status = RegistrationStatus.REGISTERED };

            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration> { reg }.BuildMock());
            _regRepoMock.Setup(r => r.UpdateAsync(1, reg)).ReturnsAsync(reg);
            _eventRepoMock.Setup(r => r.UpdateAsync(1, ev)).ReturnsAsync(ev);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            await CreateService().CancelEventAsync(1, 99, "ADMIN");
            Assert.Equal(RegistrationStatus.CANCELLED, reg.Status);
        }

        // ── GetById — paid event booked count ────────────────────────────

        [Fact]
        public async Task GetById_PaidEvent_CountsSuccessfulPayments()
        {
            var ev = new Event { EventId = 1, Title = "Paid", IsPaidEvent = true, SeatsLimit = 10 };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);

            var payments = new List<Payment>
            {
                new() { EventId = 1, Status = PaymentStatus.SUCCESS },
                new() { EventId = 1, Status = PaymentStatus.SUCCESS },
                new() { EventId = 1, Status = PaymentStatus.REFUNDED }
            };
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(payments.BuildMock());

            var result = await CreateService().GetByIdAsync(1);
            Assert.Equal(8, result.SeatsLeft); // 10 - 2 successful
        }

        // ── CreateEvent — today's date with future start time ────────────

        [Fact]
        public async Task CreateEvent_TodayWithPastStartTime_ThrowsBadRequest()
        {
            var dto = FutureEventDto();
            dto.EventDate = DateTime.UtcNow.Date;
            dto.StartTime = DateTime.UtcNow.TimeOfDay.Add(TimeSpan.FromMinutes(-30)); // 30 min ago
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateEventAsync(dto));
        }

        [Fact]
        public async Task CreateEvent_RegistrationDeadlineTodayInPast_ThrowsBadRequest()
        {
            var dto = FutureEventDto();
            dto.RegistrationDeadline = DateTime.UtcNow.Date; // today at midnight = in the past
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateEventAsync(dto));
        }

        // ── ApproveAsync — sets ApprovedByUserId ─────────────────────────

        [Fact]
        public async Task Approve_SetsApprovedByUserId()
        {
            var ev = new Event { EventId = 1, ApprovalStatus = ApprovalStatus.PENDING };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _eventRepoMock.Setup(r => r.UpdateAsync(1, ev)).ReturnsAsync(ev);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            await CreateService().ApproveAsync(1, 42);

            Assert.Equal(42, ev.ApprovedByUserId);
        }

        // ── RejectAsync — sets ApprovedByUserId ──────────────────────────

        [Fact]
        public async Task Reject_SetsApprovedByUserId()
        {
            var ev = new Event { EventId = 1, ApprovalStatus = ApprovalStatus.PENDING };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _eventRepoMock.Setup(r => r.UpdateAsync(1, ev)).ReturnsAsync(ev);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            await CreateService().RejectAsync(1, 42);

            Assert.Equal(42, ev.ApprovedByUserId);
        }

        // ── CancelEventAsync — admin, no ApprovedByUserId, skips debit ──

        [Fact]
        public async Task CancelEvent_Admin_NoApprovedByUserId_SkipsAdminDebit()
        {
            var ev = new Event
            {
                EventId = 1, Title = "E", CreatedByUserId = 5,
                ApprovedByUserId = null,  // no admin set
                IsPaidEvent = true, TicketPrice = 100f, Status = EventStatus.ACTIVE,
                EventDate = DateTime.UtcNow.Date.AddDays(5), StartTime = new TimeSpan(10, 0, 0)
            };
            var payment = new Payment
            {
                PaymentId = 1, UserId = 2, EventId = 1,
                AmountPaid = 100f, CommissionAmount = 10f, OrganizerAmount = 90f,
                Status = PaymentStatus.SUCCESS
            };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment> { payment }.BuildMock());
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, payment)).ReturnsAsync(payment);
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());
            _eventRepoMock.Setup(r => r.UpdateAsync(1, ev)).ReturnsAsync(ev);
            _walletMock.Setup(w => w.DebitAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _walletMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            await CreateService().CancelEventAsync(1, 99, "ADMIN");

            // adminId = 0 → DebitAsync for admin commission should NOT be called with adminId=0
            _walletMock.Verify(w => w.DebitAsync(0, It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            // Organizer debit IS called (orgEarning > 0)
            _walletMock.Verify(w => w.DebitAsync(5, It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        // ── GetByDateRange — empty ───────────────────────────────────────

        [Fact]
        public async Task GetByDateRange_NoEventsInRange_ReturnsEmpty()
        {
            var events = new List<Event>
            {
                new() { EventId = 1, EventDate = DateTime.UtcNow.Date.AddDays(30) }
            };
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

            var result = await CreateService().GetByDateRangeAsync(
                DateTime.UtcNow.Date,
                DateTime.UtcNow.Date.AddDays(7));
            Assert.Empty(result);
        }

        // ── GetMyEvents — empty ──────────────────────────────────────────

        [Fact]
        public async Task GetMyEvents_NoEvents_ReturnsEmpty()
        {
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Event>().BuildMock());
            var result = await CreateService().GetMyEventsAsync(99);
            Assert.Empty(result);
        }

        // ── GetRegisteredEvents — empty ──────────────────────────────────

        [Fact]
        public async Task GetRegisteredEvents_NoRegistrations_ReturnsEmpty()
        {
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());
            var result = await CreateService().GetRegisteredEventsAsync(99);
            Assert.Empty(result);
        }

        // ── CancelEventAsync — organizer late cancel penalty ─────────────

        [Fact]
        public async Task CancelEvent_Organizer_LateCancel_PenaltyRefund()
        {
            // < 48h before start → organizer pays penalty (2% admin + rest from org)
            var ev = new Event
            {
                EventId = 1,
                Title = "Late",
                CreatedByUserId = 1,
                ApprovedByUserId = 10,
                IsPaidEvent = true,
                TicketPrice = 100f,
                Status = EventStatus.ACTIVE,
                EventDate = DateTime.UtcNow.Date,
                StartTime = DateTime.UtcNow.TimeOfDay.Add(TimeSpan.FromHours(24)) // 24h → < 48h
            };
            var payment = new Payment
            {
                PaymentId = 1, UserId = 2, EventId = 1,
                AmountPaid = 100f, CommissionAmount = 10f, OrganizerAmount = 90f,
                Status = PaymentStatus.SUCCESS
            };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _paymentRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Payment> { payment }.BuildMock());
            _paymentRepoMock.Setup(r => r.UpdateAsync(1, payment)).ReturnsAsync(payment);
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());
            _eventRepoMock.Setup(r => r.UpdateAsync(1, ev)).ReturnsAsync(ev);
            _walletMock.Setup(w => w.DebitAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _walletMock.Setup(w => w.CreditAsync(It.IsAny<int>(), It.IsAny<float>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            await CreateService().CancelEventAsync(1, 1, "ORGANIZER");

            Assert.Equal(EventStatus.CANCELLED, ev.Status);
            Assert.Equal(PaymentStatus.REFUNDED, payment.Status);
            Assert.Equal("ORGANIZER", payment.CancelledBy);
        }

        // ── GetAllAsync — free event uses registration count ─────────────

        [Fact]
        public async Task GetAll_FreeEvent_CountsRegistrations()
        {
            var events = new List<Event>
            {
                new()
                {
                    EventId = 1, Title = "Free", IsPaidEvent = false,
                    Status = EventStatus.ACTIVE, ApprovalStatus = ApprovalStatus.APPROVED,
                    SeatsLimit = 5, CreatedBy = new User()
                }
            };
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

            var registrations = new List<EventRegistration>
            {
                new() { EventId = 1, Status = RegistrationStatus.REGISTERED },
                new() { EventId = 1, Status = RegistrationStatus.REGISTERED },
                new() { EventId = 1, Status = RegistrationStatus.CANCELLED }
            };
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(registrations.BuildMock());
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());

            var result = (await CreateService().GetAllAsync()).ToList();

            Assert.Single(result);
            Assert.Equal(3, result[0].SeatsLeft); // 5 - 2 registered
        }

        // ── MapToDTO — SeatsLimit null → SeatsLeft = -1 ──────────────────

        [Fact]
        public async Task GetById_NoSeatsLimit_SeatsLeftIsMinusOne()
        {
            var ev = new Event { EventId = 1, Title = "Unlimited", IsPaidEvent = false, SeatsLimit = null };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());

            var result = await CreateService().GetByIdAsync(1);
            Assert.Equal(-1, result.SeatsLeft);
        }

        // ── MapToDTO — no StartTime/EndTime → null strings ───────────────

        [Fact]
        public async Task GetById_NoStartEndTime_NullInDTO()
        {
            var ev = new Event { EventId = 1, Title = "E", IsPaidEvent = false, StartTime = null, EndTime = null };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());

            var result = await CreateService().GetByIdAsync(1);
            Assert.Null(result.StartTime);
            Assert.Null(result.EndTime);
        }

        // ── CreateEvent — audit log action ───────────────────────────────

        [Fact]
        public async Task CreateEvent_AuditLog_HasCorrectAction()
        {
            var dto = FutureEventDto();
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Event>().BuildMock());
            _eventRepoMock.Setup(r => r.AddAsync(It.IsAny<Event>()))
                .ReturnsAsync(new Event { EventId = 1, Title = "Test Event" });

            AuditLog? captured = null;
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>()))
                .Callback<AuditLog>(log => captured = log)
                .ReturnsAsync(new AuditLog());

            await CreateService().CreateEventAsync(dto);

            Assert.Equal("CREATE_EVENT", captured?.Action);
            Assert.Equal("ORGANIZER", captured?.Role);
        }

        // ── GetExpiredEventsAsync — EventEndDate path ────────────────────

        [Fact]
        public async Task GetExpiredEvents_WithEventEndDate_UsesEndDate()
        {
            // EventDate is in the future but EventEndDate is in the past → expired
            var events = new List<Event>
            {
                new()
                {
                    EventId = 1,
                    EventDate = DateTime.UtcNow.Date.AddDays(5),   // future start
                    EventEndDate = DateTime.UtcNow.Date.AddDays(-1), // past end
                    IsPaidEvent = false,
                    CreatedBy = new User()
                }
            };
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());
            _regRepoMock.Setup(r => r.GetQueryable()).Returns(new List<EventRegistration>().BuildMock());
            _paymentRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Payment>().BuildMock());

            var result = (await CreateService().GetExpiredEventsAsync()).ToList();
            Assert.Single(result);
        }

        // ── GetAllEventsPagedAsync — location search ─────────────────────

        [Fact]
        public async Task GetAllEventsPaged_SearchByLocation_FiltersCorrectly()
        {
            var events = new List<Event>
            {
                new() { EventId = 1, Title = "Concert", Location = "Mumbai Arena", EventDate = DateTime.UtcNow.Date, CreatedBy = new User() },
                new() { EventId = 2, Title = "Workshop", Location = "Delhi Hub", EventDate = DateTime.UtcNow.Date, CreatedBy = new User() }
            };
            _eventRepoMock.Setup(r => r.GetQueryable()).Returns(events.BuildMock());

            var result = await CreateService().GetAllEventsPagedAsync(1, 10, "Mumbai");
            Assert.Equal(1, result.TotalRecords);
            Assert.Equal("Concert", result.Data.First().Title);
        }
    }
}






