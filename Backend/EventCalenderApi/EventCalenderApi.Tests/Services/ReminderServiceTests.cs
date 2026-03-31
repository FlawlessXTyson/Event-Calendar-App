using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Remainder;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Services;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace EventCalenderApi.Tests.Services
{
    public class ReminderServiceTests
    {
        private readonly Mock<IRepository<int, Reminder>> _repoMock = new();
        private readonly Mock<IRepository<int, Event>> _eventRepoMock = new();
        private ReminderService CreateService() => new(_repoMock.Object, _eventRepoMock.Object);

        // ── CreateAsync — manual datetime ────────────────────────────────

        [Fact]
        public async Task Create_ManualDateTime_Success()
        {
            var dto = new CreateReminderRequestDTO
            {
                ReminderTitle = "Test",
                ReminderDateTime = DateTime.UtcNow.AddHours(2)
            };

            _repoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Reminder>().BuildMock());
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Reminder>()))
                .ReturnsAsync(new Reminder { ReminderId = 1, UserId = 1, ReminderTitle = "Test" });

            var result = await CreateService().CreateAsync(dto, 1);
            Assert.Equal("Test", result.ReminderTitle);
        }

        [Fact]
        public async Task Create_NullDto_ThrowsBadRequest()
        {
            await Assert.ThrowsAsync<BadRequestException>(() =>
                CreateService().CreateAsync(null!, 1));
        }

        [Fact]
        public async Task Create_EmptyTitle_ThrowsBadRequest()
        {
            var dto = new CreateReminderRequestDTO
            {
                ReminderTitle = "  ",
                ReminderDateTime = DateTime.UtcNow.AddHours(1)
            };
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(dto, 1));
        }

        [Fact]
        public async Task Create_BothDateTimeAndMinutesBefore_ThrowsBadRequest()
        {
            var dto = new CreateReminderRequestDTO
            {
                ReminderTitle = "Test",
                ReminderDateTime = DateTime.UtcNow.AddHours(1),
                MinutesBefore = 30,
                EventId = 1
            };
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(dto, 1));
        }

        [Fact]
        public async Task Create_PastManualDateTime_ThrowsBadRequest()
        {
            var dto = new CreateReminderRequestDTO
            {
                ReminderTitle = "Test",
                ReminderDateTime = DateTime.UtcNow.AddHours(-1)
            };
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(dto, 1));
        }

        [Fact]
        public async Task Create_NeitherDateTimeNorMinutesBefore_ThrowsBadRequest()
        {
            var dto = new CreateReminderRequestDTO { ReminderTitle = "Test" };
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(dto, 1));
        }

        // ── CreateAsync — event-based ────────────────────────────────────

        [Fact]
        public async Task Create_EventBased_Success()
        {
            var ev = new Event
            {
                EventId = 1,
                EventDate = DateTime.UtcNow.Date.AddDays(2),
                StartTime = new TimeSpan(10, 0, 0)
            };
            var dto = new CreateReminderRequestDTO
            {
                ReminderTitle = "Event Reminder",
                EventId = 1,
                MinutesBefore = 30
            };

            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            _repoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Reminder>().BuildMock());
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Reminder>()))
                .ReturnsAsync(new Reminder { ReminderId = 1, UserId = 1, ReminderTitle = "Event Reminder" });

            var result = await CreateService().CreateAsync(dto, 1);
            Assert.Equal("Event Reminder", result.ReminderTitle);
        }

        [Fact]
        public async Task Create_EventBased_ZeroMinutesBefore_ThrowsBadRequest()
        {
            var dto = new CreateReminderRequestDTO
            {
                ReminderTitle = "Test",
                EventId = 1,
                MinutesBefore = 0
            };
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(dto, 1));
        }

        [Fact]
        public async Task Create_EventBased_EventNotFound_ThrowsNotFound()
        {
            var dto = new CreateReminderRequestDTO
            {
                ReminderTitle = "Test",
                EventId = 99,
                MinutesBefore = 30
            };
            _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().CreateAsync(dto, 1));
        }

        [Fact]
        public async Task Create_EventBased_NoStartTime_ThrowsBadRequest()
        {
            var ev = new Event { EventId = 1, EventDate = DateTime.UtcNow.Date.AddDays(2), StartTime = null };
            var dto = new CreateReminderRequestDTO { ReminderTitle = "Test", EventId = 1, MinutesBefore = 30 };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(dto, 1));
        }

        [Fact]
        public async Task Create_EventBased_CalculatedTimeInPast_ThrowsBadRequest()
        {
            // Event starts in 10 minutes, but MinutesBefore = 30 → calculated time is in the past
            var ev = new Event
            {
                EventId = 1,
                EventDate = DateTime.UtcNow.Date,
                StartTime = DateTime.UtcNow.TimeOfDay.Add(TimeSpan.FromMinutes(10))
            };
            var dto = new CreateReminderRequestDTO { ReminderTitle = "Test", EventId = 1, MinutesBefore = 30 };
            _eventRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ev);
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(dto, 1));
        }

        // ── GetByUserAsync ───────────────────────────────────────────────

        [Fact]
        public async Task GetByUser_ReturnsUserReminders()
        {
            var reminders = new List<Reminder>
            {
                new() { ReminderId = 1, UserId = 1, ReminderTitle = "R1" },
                new() { ReminderId = 2, UserId = 2, ReminderTitle = "R2" }
            };
            _repoMock.Setup(r => r.GetQueryable()).Returns(reminders.BuildMock());

            var result = (await CreateService().GetByUserAsync(1)).ToList();
            Assert.Single(result);
        }

        // ── DeleteAsync ──────────────────────────────────────────────────

        [Fact]
        public async Task Delete_ValidOwner_Deletes()
        {
            var reminder = new Reminder { ReminderId = 1, UserId = 1 };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reminder);
            _repoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(reminder);

            await CreateService().DeleteAsync(1, 1); // no exception = pass
        }

        [Fact]
        public async Task Delete_NotFound_ThrowsNotFound()
        {
            _repoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Reminder?)null);
            await Assert.ThrowsAsync<NotFoundException>(() => CreateService().DeleteAsync(99, 1));
        }

        [Fact]
        public async Task Delete_WrongUser_ThrowsUnauthorized()
        {
            var reminder = new Reminder { ReminderId = 1, UserId = 2 };
            _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(reminder);
            await Assert.ThrowsAsync<UnauthorizedException>(() => CreateService().DeleteAsync(1, 1));
        }

        // ── GetDueRemindersAsync ─────────────────────────────────────────

        [Fact]
        public async Task GetDueReminders_ReturnsWithinWindow()
        {
            var now = DateTime.UtcNow;
            var reminders = new List<Reminder>
            {
                new() { ReminderId = 1, UserId = 1, ReminderTitle = "Due", ReminderDateTime = now.AddSeconds(-10) },
                new() { ReminderId = 2, UserId = 1, ReminderTitle = "Future", ReminderDateTime = now.AddHours(1) },
                new() { ReminderId = 3, UserId = 1, ReminderTitle = "Old", ReminderDateTime = now.AddMinutes(-5) }
            };
            _repoMock.Setup(r => r.GetQueryable()).Returns(reminders.BuildMock());

            var result = (await CreateService().GetDueRemindersAsync(1)).ToList();
            Assert.Single(result);
            Assert.Equal("Due", result[0].ReminderTitle);
        }

        // ── CreateAsync — duplicate & DbUpdateException ──────────────────

        [Fact]
        public async Task Create_DbUpdateException_ThrowsBadRequest()
        {
            var dto = new CreateReminderRequestDTO
            {
                ReminderTitle = "Test",
                ReminderDateTime = DateTime.UtcNow.AddHours(2)
            };

            _repoMock.Setup(r => r.GetQueryable())
                .Returns(new List<Reminder>().BuildMock());
            _repoMock.Setup(r => r.AddAsync(It.IsAny<Reminder>()))
                .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("Duplicate", new Exception()));

            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().CreateAsync(dto, 1));
        }

        [Fact]
        public async Task GetByUser_NoReminders_ReturnsEmpty()
        {
            _repoMock.Setup(r => r.GetQueryable()).Returns(new List<Reminder>().BuildMock());
            var result = await CreateService().GetByUserAsync(99);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetDueReminders_NoRemindersInWindow_ReturnsEmpty()
        {
            var reminders = new List<Reminder>
            {
                new() { ReminderId = 1, UserId = 1, ReminderTitle = "Future", ReminderDateTime = DateTime.UtcNow.AddHours(1) }
            };
            _repoMock.Setup(r => r.GetQueryable()).Returns(reminders.BuildMock());

            var result = await CreateService().GetDueRemindersAsync(1);
            Assert.Empty(result);
        }
    }
}





