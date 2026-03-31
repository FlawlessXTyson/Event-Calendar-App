using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Services;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace EventCalenderApi.Tests.Services
{
    public class AuditLogServiceTests
    {
        private readonly Mock<IAuditLogRepository> _repoMock = new();
        private readonly Mock<IRepository<int, User>> _userRepoMock = new();
        private AuditLogService CreateService() => new(_repoMock.Object, _userRepoMock.Object);

        private List<AuditLog> SampleLogs() => new()
        {
            new() { Id = 1, UserId = 1, Action = "LOGIN",    Entity = "User",    Role = "USER",  CreatedAt = DateTime.UtcNow },
            new() { Id = 2, UserId = 2, Action = "REGISTER", Entity = "User",    Role = "USER",  CreatedAt = DateTime.UtcNow },
            new() { Id = 3, UserId = 1, Action = "CREATE_EVENT", Entity = "Event", Role = "ORGANIZER", CreatedAt = DateTime.UtcNow }
        };

        private List<User> SampleUsers() => new()
        {
            new() { UserId = 1, Name = "Alice" },
            new() { UserId = 2, Name = "Bob" }
        };

        // ── GetAllAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_ReturnsAllLogs()
        {
            _repoMock.Setup(r => r.GetQueryable()).Returns(SampleLogs().BuildMock());
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(SampleUsers().BuildMock());

            var result = (await CreateService().GetAllAsync()).ToList();

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_MapsUserName_Correctly()
        {
            _repoMock.Setup(r => r.GetQueryable()).Returns(SampleLogs().BuildMock());
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(SampleUsers().BuildMock());

            var result = (await CreateService().GetAllAsync()).ToList();

            Assert.Contains(result, r => r.UserName == "Alice");
            Assert.Contains(result, r => r.UserName == "Bob");
        }

        [Fact]
        public async Task GetAllAsync_UnknownUser_ReturnsEmptyName()
        {
            var logs = new List<AuditLog>
            {
                new() { Id = 1, UserId = 999, Action = "LOGIN", Entity = "User", Role = "USER", CreatedAt = DateTime.UtcNow }
            };
            _repoMock.Setup(r => r.GetQueryable()).Returns(logs.BuildMock());
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(new List<User>().BuildMock());

            var result = (await CreateService().GetAllAsync()).ToList();
            Assert.Equal(string.Empty, result[0].UserName);
        }

        // ── GetByUserIdAsync ─────────────────────────────────────────────

        [Fact]
        public async Task GetByUserIdAsync_FiltersCorrectly()
        {
            _repoMock.Setup(r => r.GetQueryable()).Returns(SampleLogs().BuildMock());
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(SampleUsers().BuildMock());

            var result = (await CreateService().GetByUserIdAsync(1)).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(1, r.UserId));
        }

        [Fact]
        public async Task GetByUserIdAsync_NoMatch_ReturnsEmpty()
        {
            _repoMock.Setup(r => r.GetQueryable()).Returns(SampleLogs().BuildMock());
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(SampleUsers().BuildMock());

            var result = await CreateService().GetByUserIdAsync(999);
            Assert.Empty(result);
        }

        // ── GetByEntityAsync ─────────────────────────────────────────────

        [Fact]
        public async Task GetByEntityAsync_CaseInsensitive_FiltersCorrectly()
        {
            _repoMock.Setup(r => r.GetQueryable()).Returns(SampleLogs().BuildMock());
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(SampleUsers().BuildMock());

            var result = (await CreateService().GetByEntityAsync("user")).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetByEntityAsync_NoMatch_ReturnsEmpty()
        {
            _repoMock.Setup(r => r.GetQueryable()).Returns(SampleLogs().BuildMock());
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(SampleUsers().BuildMock());

            var result = await CreateService().GetByEntityAsync("Payment");
            Assert.Empty(result);
        }

        // ── GetByActionAsync ─────────────────────────────────────────────

        [Fact]
        public async Task GetByActionAsync_CaseInsensitive_FiltersCorrectly()
        {
            _repoMock.Setup(r => r.GetQueryable()).Returns(SampleLogs().BuildMock());
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(SampleUsers().BuildMock());

            var result = (await CreateService().GetByActionAsync("login")).ToList();

            Assert.Single(result);
            Assert.Equal("LOGIN", result[0].Action);
        }

        [Fact]
        public async Task GetByActionAsync_NoMatch_ReturnsEmpty()
        {
            _repoMock.Setup(r => r.GetQueryable()).Returns(SampleLogs().BuildMock());
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(SampleUsers().BuildMock());

            var result = await CreateService().GetByActionAsync("DELETE");
            Assert.Empty(result);
        }

        // ── GetAllAsync — empty ──────────────────────────────────────────

        [Fact]
        public async Task GetAllAsync_NoLogs_ReturnsEmpty()
        {
            _repoMock.Setup(r => r.GetQueryable()).Returns(new List<AuditLog>().BuildMock());
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(new List<User>().BuildMock());

            var result = await CreateService().GetAllAsync();
            Assert.Empty(result);
        }

        // ── GetAllAsync — ordered descending by CreatedAt ────────────────

        [Fact]
        public async Task GetAllAsync_OrderedDescendingByCreatedAt()
        {
            var logs = new List<AuditLog>
            {
                new() { Id = 1, UserId = 1, Action = "A", Entity = "User", Role = "USER", CreatedAt = DateTime.UtcNow.AddHours(-2) },
                new() { Id = 2, UserId = 1, Action = "B", Entity = "User", Role = "USER", CreatedAt = DateTime.UtcNow },
                new() { Id = 3, UserId = 1, Action = "C", Entity = "User", Role = "USER", CreatedAt = DateTime.UtcNow.AddHours(-1) }
            };
            _repoMock.Setup(r => r.GetQueryable()).Returns(logs.BuildMock());
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(SampleUsers().BuildMock());

            var result = (await CreateService().GetAllAsync()).ToList();

            // Most recent first
            Assert.Equal("B", result[0].Action);
            Assert.Equal("C", result[1].Action);
            Assert.Equal("A", result[2].Action);
        }

        // ── GetByEntityAsync — exact case match ──────────────────────────

        [Fact]
        public async Task GetByEntityAsync_UppercaseInput_StillMatches()
        {
            _repoMock.Setup(r => r.GetQueryable()).Returns(SampleLogs().BuildMock());
            _userRepoMock.Setup(r => r.GetQueryable()).Returns(SampleUsers().BuildMock());

            var result = (await CreateService().GetByEntityAsync("EVENT")).ToList();
            Assert.Single(result);
        }
    }
}





