using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Services;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace EventCalenderApi.Tests.Services
{
    public class TicketServiceTests
    {
        private readonly Mock<IRepository<int, Ticket>> _ticketRepoMock = new();
        private readonly Mock<IRepository<int, Event>> _eventRepoMock = new();
        private readonly Mock<IRepository<int, User>> _userRepoMock = new();
        private readonly Mock<IRepository<int, Payment>> _paymentRepoMock = new();
        private readonly Mock<IAuditLogRepository> _auditMock = new();

        private TicketService CreateService() => new(
            _ticketRepoMock.Object,
            _eventRepoMock.Object,
            _userRepoMock.Object,
            _paymentRepoMock.Object,
            _auditMock.Object);

        private static Event SampleEvent() => new()
        {
            EventId = 1,
            Title = "Test Event",
            EventDate = DateTime.UtcNow.Date.AddDays(1),
            StartTime = new TimeSpan(10, 0, 0)
        };

        private static User SampleUser() => new() { UserId = 1, Name = "Alice" };

        // ── GenerateTicketAsync ──────────────────────────────────────────

        [Fact]
        public async Task GenerateTicket_NewTicket_CreatesAndReturns()
        {
            var noExisting = new List<Ticket>();
            _ticketRepoMock.SetupSequence(r => r.GetQueryable())
                .Returns(noExisting.BuildMock())  // first call: check existing
                .Returns(new List<Ticket>                              // second call: reload with nav props
                {
                    new() { TicketId = 1, UserId = 1, EventId = 1, User = SampleUser(), Event = SampleEvent() }
                }.BuildMock());

            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleEvent());
            _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleUser());
            _ticketRepoMock.Setup(r => r.AddAsync(It.IsAny<Ticket>()))
                .ReturnsAsync(new Ticket { TicketId = 1, UserId = 1, EventId = 1 });
            _auditMock.Setup(a => a.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());

            var result = await CreateService().GenerateTicketAsync(1, 1, null);
            Assert.Equal(1, result.TicketId);
        }

        [Fact]
        public async Task GenerateTicket_ExistingTicket_ReturnsExisting()
        {
            var existing = new List<Ticket>
            {
                new() { TicketId = 5, UserId = 1, EventId = 1, User = SampleUser(), Event = SampleEvent() }
            };
            _ticketRepoMock.Setup(r => r.GetQueryable()).Returns(existing.BuildMock());

            var result = await CreateService().GenerateTicketAsync(1, 1, null);
            Assert.Equal(5, result.TicketId);
        }

        [Fact]
        public async Task GenerateTicket_EventNotFound_ThrowsNotFound()
        {
            _ticketRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Ticket>().BuildMock());
            _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                CreateService().GenerateTicketAsync(1, 99, null));
        }

        [Fact]
        public async Task GenerateTicket_UserNotFound_ThrowsNotFound()
        {
            _ticketRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Ticket>().BuildMock());
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleEvent());
            _userRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                CreateService().GenerateTicketAsync(99, 1, null));
        }

        // ── GetTicketAsync ───────────────────────────────────────────────

        [Fact]
        public async Task GetTicket_Found_ReturnsTicket()
        {
            var tickets = new List<Ticket>
            {
                new() { TicketId = 1, UserId = 1, EventId = 1, User = SampleUser(), Event = SampleEvent() }
            };
            _ticketRepoMock.Setup(r => r.GetQueryable()).Returns(tickets.BuildMock());

            var result = await CreateService().GetTicketAsync(1, 1);
            Assert.NotNull(result);
            Assert.Equal(1, result!.TicketId);
        }

        [Fact]
        public async Task GetTicket_NotFound_ReturnsNull()
        {
            _ticketRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Ticket>().BuildMock());

            var result = await CreateService().GetTicketAsync(1, 1);
            Assert.Null(result);
        }

        // ── GetMyTicketsAsync ────────────────────────────────────────────

        [Fact]
        public async Task GetMyTickets_ReturnsUserTickets()
        {
            var tickets = new List<Ticket>
            {
                new() { TicketId = 1, UserId = 1, EventId = 1, GeneratedAt = DateTime.UtcNow, User = SampleUser(), Event = SampleEvent() },
                new() { TicketId = 2, UserId = 2, EventId = 1, GeneratedAt = DateTime.UtcNow, User = new User { UserId = 2 }, Event = SampleEvent() }
            };
            _ticketRepoMock.Setup(r => r.GetQueryable()).Returns(tickets.BuildMock());

            var result = (await CreateService().GetMyTicketsAsync(1)).ToList();
            Assert.Single(result);
            Assert.Equal(1, result[0].UserId);
        }

        [Fact]
        public async Task GetMyTickets_NoTickets_ReturnsEmpty()
        {
            _ticketRepoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Ticket>().BuildMock());

            var result = await CreateService().GetMyTicketsAsync(99);
            Assert.Empty(result);
        }
    }
}





