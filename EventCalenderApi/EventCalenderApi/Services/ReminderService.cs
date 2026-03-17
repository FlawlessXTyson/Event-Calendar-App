using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Remainder;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EventCalenderApi.Services
{
    public class ReminderService : IReminderService
    {
        private readonly IRepository<int, Reminder> _repo;

        public ReminderService(IRepository<int, Reminder> repo)
        {
            _repo = repo;
        }

        //create reminder
        public async Task<CreateReminderResponseDTO> CreateAsync(CreateReminderRequestDTO dto)
        {
            var reminder = new Reminder
            {
                UserId = dto.UserId,
                EventId = dto.EventId,
                ReminderTitle = dto.ReminderTitle,
                ReminderDateTime = dto.ReminderDateTime,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repo.AddAsync(reminder);

            return new CreateReminderResponseDTO
            {
                ReminderId = created.ReminderId,
                UserId = created.UserId,
                EventId = created.EventId,
                ReminderTitle = created.ReminderTitle,
                ReminderDateTime = created.ReminderDateTime,
                CreatedAt = created.CreatedAt
            };
        }

        //get reminders by user
        public async Task<IEnumerable<CreateReminderResponseDTO>> GetByUserAsync(int userId)
        {
            var reminders = await _repo
                .GetQueryable()
                .Where(r => r.UserId == userId)
                .ToListAsync();

            return reminders.Select(r => new CreateReminderResponseDTO
            {
                ReminderId = r.ReminderId,
                UserId = r.UserId,
                EventId = r.EventId,
                ReminderTitle = r.ReminderTitle,
                ReminderDateTime = r.ReminderDateTime,
                CreatedAt = r.CreatedAt
            });
        }

        //delete reminder
        public async Task DeleteAsync(int reminderId)
        {
            var reminder = await _repo.GetByIdAsync(reminderId);

            if (reminder == null)
                throw new NotFoundException("Reminder not found");

            await _repo.DeleteAsync(reminderId);
        }
    }
}