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

        //  CREATE REMINDER (FINAL FIXED)
        public async Task<CreateReminderResponseDTO> CreateAsync(CreateReminderRequestDTO dto, int userId)
        {
            if (dto == null)
                throw new BadRequestException("Request body cannot be null");

            if (string.IsNullOrWhiteSpace(dto.ReminderTitle))
                throw new BadRequestException("Reminder title is required");

            //  FIX 1: DO NOT ALLOW BOTH
            if (dto.ReminderDateTime != null && dto.MinutesBefore != null)
                throw new BadRequestException("Provide either ReminderDateTime OR MinutesBefore, not both");

            DateTime finalReminderTime;

            // 🔹 CASE 1: Manual
            if (dto.ReminderDateTime != null)
            {
                finalReminderTime = dto.ReminderDateTime.Value;

                if (finalReminderTime <= DateTime.UtcNow)
                    throw new BadRequestException("Reminder time must be in the future");
            }

            // 🔹 CASE 2: Event-based
            else if (dto.EventId != null && dto.MinutesBefore != null)
            {
                if (dto.MinutesBefore <= 0)
                    throw new BadRequestException("MinutesBefore must be greater than zero");

                var ev = await _eventRepo.GetByIdAsync(dto.EventId.Value)
                    ?? throw new NotFoundException("Event not found");

                if (ev.StartTime == null)
                    throw new BadRequestException("Event start time is missing");

                var eventDateTime = ev.EventDate.Date + ev.StartTime.Value;

                finalReminderTime = eventDateTime.AddMinutes(-dto.MinutesBefore.Value);

                if (finalReminderTime <= DateTime.UtcNow)
                    throw new BadRequestException("Calculated reminder time is in the past");
            }

            else
            {
                throw new BadRequestException("Provide either ReminderDateTime or MinutesBefore");
            }

            //  FIX 2: SAFE DUPLICATE CHECK (NO MILLISECOND ISSUE)
            var alreadyExists = await _repo
                .GetQueryable()
                .AnyAsync(r =>
                    r.UserId == userId &&
                    r.EventId == dto.EventId &&
                    r.ReminderTitle.ToLower() == dto.ReminderTitle.ToLower() &&
                    EF.Functions.DateDiffSecond(r.ReminderDateTime, finalReminderTime) == 0
                );

            if (alreadyExists)
                throw new BadRequestException("Reminder already exists with same time");

            var reminder = new Reminder
            {
                UserId = userId,
                EventId = dto.EventId,
                ReminderTitle = dto.ReminderTitle,
                ReminderDateTime = finalReminderTime,
                CreatedAt = DateTime.UtcNow
            };

            //  FIX 3: HANDLE DB UNIQUE ERROR
            try
            {
                var created = await _repo.AddAsync(reminder);
                return MapToDTO(created);
            }
            catch (DbUpdateException)
            {
                throw new BadRequestException("Duplicate reminder detected");
            }
        }

        // GET USER REMINDERS
        public async Task<IEnumerable<CreateReminderResponseDTO>> GetByUserAsync(int userId)
        {
            var reminders = await _repo
                .GetQueryable()
                .Where(r => r.UserId == userId)
                .ToListAsync();

            return reminders.Select(MapToDTO);
        }

        // DELETE
        public async Task DeleteAsync(int reminderId, int userId)
        {
            var reminder = await _repo.GetByIdAsync(reminderId)
                ?? throw new NotFoundException("Reminder not found");

            if (reminder.UserId != userId)
                throw new UnauthorizedException("You can delete only your own reminders");

            await _repo.DeleteAsync(reminderId);
        }

        // ================= DUE REMINDERS =================
        public async Task<IEnumerable<CreateReminderResponseDTO>> GetDueRemindersAsync(int userId)
        {
            var now = DateTime.UtcNow;

            var reminders = await _repo
                .GetQueryable()
                .Where(r =>
                    r.UserId == userId &&
                    r.ReminderDateTime <= now &&
                    r.ReminderDateTime >= now.AddMinutes(-1)) // 🔥 last 1 min window
                .ToListAsync();

            return reminders.Select(MapToDTO);
        }

        private static CreateReminderResponseDTO MapToDTO(Reminder r)
        {
            return new CreateReminderResponseDTO
            {
                ReminderId = r.ReminderId,
                UserId = r.UserId,
                EventId = r.EventId,
                ReminderTitle = r.ReminderTitle,
                ReminderDateTime = r.ReminderDateTime,
                CreatedAt = r.CreatedAt
            };
        }
    }
}