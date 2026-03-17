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
        private readonly IRepository<int, Event> _eventRepo;

        public ReminderService(
            IRepository<int, Reminder> repo,
            IRepository<int, Event> eventRepo)
        {
            _repo = repo;
            _eventRepo = eventRepo;
        }

        //create reminder
        public async Task<CreateReminderResponseDTO> CreateAsync(CreateReminderRequestDTO dto, int userId)
        {
            DateTime finalReminderTime;

            //manual reminder
            if (dto.ReminderDateTime != null)
            {
                finalReminderTime = dto.ReminderDateTime.Value;
            }
            //auto reminder (before event)
            else if (dto.EventId != null && dto.MinutesBefore != null)
            {
                var ev = await _eventRepo.GetByIdAsync(dto.EventId.Value);

                if (ev == null)
                    throw new NotFoundException("Event not found");

                if (ev.StartTime == null)
                    throw new BadRequestException("Event start time is missing");

                var eventDateTime = ev.EventDate.Date + ev.StartTime.Value;

                finalReminderTime = eventDateTime.AddMinutes(-dto.MinutesBefore.Value);
            }
            else
            {
                throw new BadRequestException("Provide either ReminderDateTime or MinutesBefore");
            }

            var reminder = new Reminder
            {
                UserId = userId,
                EventId = dto.EventId,
                ReminderTitle = dto.ReminderTitle,
                ReminderDateTime = finalReminderTime,
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