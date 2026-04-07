using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.Services;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace Tests.Services;

public class TicketServiceTests
{
    private readonly Mock<IRepository<int, Ticket>> _ticketRepoMock = new();
    private readonly Mock<IRepository<int, Event>> _eventRepoMock = new();
    private readonly Mock<IRepository<int, User>> _userRepoMock = new();
    private readonly Mock<IRepository<int, Payment>> _paymentRepoMock = new();
    private readonly Mock<IAuditLogRepository> _auditRepoMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly TicketService _sut;

    public TicketServiceTests()
    {
        _sut = new TicketService(
            _ticketRepoMock.Object,
            _eventRepoMock.Object,
            _userRepoMock.Object,
            _paymentRepoMock.Object,
            _auditRepoMock.Object,
            _emailServiceMock.Object);
        _auditRepoMock.Setup(r => r.AddAsync(It.IsAny<AuditLog>())).ReturnsAsync(new AuditLog());
    }

    private static Event SampleEvent() => new Event
    {
        EventId = 1,
        Title = "Tech Conf",
        EventDate = DateTime.UtcNow.AddDays(5),
        StartTime = new TimeSpan(9, 0, 0),
        EndTime = new TimeSpan(17, 0, 0),
        Location = "Hall A",
        IsPaidEvent = true
    };

    private static User SampleUser() => new User { UserId = 1, Name = "Alice", Email = "alice@example.com" };

    // ── GenerateTicketAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GenerateTicketAsync_Should_ReturnExistingTicket_When_AlreadyGenerated()
    {
        // Arrange
        var existing = new Ticket { TicketId = 1, UserId = 1, EventId = 1, User = SampleUser(), Event = SampleEvent() };
        _ticketRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Ticket> { existing }.BuildMock());

        // Act
        var result = await _sut.GenerateTicketAsync(userId: 1, eventId: 1, paymentId: null);

        // Assert
        Assert.Equal(1, result.TicketId);
        _ticketRepoMock.Verify(r => r.AddAsync(It.IsAny<Ticket>()), Times.Never);
    }

    [Fact]
    public async Task GenerateTicketAsync_Should_CreateTicket_When_NotYetGenerated()
    {
        // Arrange
        var emptyTickets = new List<Ticket>();
        var createdTicket = new Ticket { TicketId = 2, UserId = 1, EventId = 1, User = SampleUser(), Event = SampleEvent() };

        // First call returns empty (no existing), second call returns the created ticket
        var callCount = 0;
        _ticketRepoMock.Setup(r => r.GetQueryable()).Returns(() =>
        {
            callCount++;
            return callCount == 1
                ? emptyTickets.BuildMock()
                : new List<Ticket> { createdTicket }.BuildMock();
        });

        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleEvent());
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleUser());
        _ticketRepoMock.Setup(r => r.AddAsync(It.IsAny<Ticket>()))
            .ReturnsAsync((Ticket t) => { t.TicketId = 2; return t; });

        // Act
        var result = await _sut.GenerateTicketAsync(userId: 1, eventId: 1, paymentId: null);

        // Assert
        Assert.Equal(2, result.TicketId);
        _ticketRepoMock.Verify(r => r.AddAsync(It.IsAny<Ticket>()), Times.Once);
    }

    [Fact]
    public async Task GenerateTicketAsync_Should_ThrowNotFound_When_EventDoesNotExist()
    {
        // Arrange
        _ticketRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Ticket>().BuildMock());
        _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GenerateTicketAsync(1, 99, null));
    }

    [Fact]
    public async Task GenerateTicketAsync_Should_ThrowNotFound_When_UserDoesNotExist()
    {
        // Arrange
        _ticketRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Ticket>().BuildMock());
        _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(SampleEvent());
        _userRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GenerateTicketAsync(99, 1, null));
    }

    // ── GetTicketAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetTicketAsync_Should_ReturnTicket_When_TicketExists()
    {
        // Arrange
        var ticket = new Ticket { TicketId = 1, UserId = 1, EventId = 1, User = SampleUser(), Event = SampleEvent() };
        _ticketRepoMock.Setup(r => r.GetQueryable())
            .Returns(new List<Ticket> { ticket }.BuildMock());

        // Act
        var result = await _sut.GetTicketAsync(1, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result!.TicketId);
    }

    [Fact]
    public async Task GetTicketAsync_Should_ReturnNull_When_TicketDoesNotExist()
    {
        // Arrange
        _ticketRepoMock.Setup(r => r.GetQueryable()).Returns(new List<Ticket>().BuildMock());

        // Act
        var result = await _sut.GetTicketAsync(1, 1);

        // Assert
        Assert.Null(result);
    }

    // ── GetMyTicketsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetMyTicketsAsync_Should_ReturnUserTickets()
    {
        // Arrange
        var tickets = new List<Ticket>
        {
            new Ticket { TicketId = 1, UserId = 1, EventId = 1, User = SampleUser(), Event = SampleEvent() },
            new Ticket { TicketId = 2, UserId = 2, EventId = 2, User = new User { UserId = 2 }, Event = SampleEvent() }
        };
        _ticketRepoMock.Setup(r => r.GetQueryable()).Returns(tickets.BuildMock());

        // Act
        var result = (await _sut.GetMyTicketsAsync(1)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].UserId);
    }
}

