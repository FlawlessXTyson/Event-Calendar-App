using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Event;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.EntityFrameworkCore;

namespace EventCalenderApi.Services
{
    public class EventService : IEventService
    {
        private readonly IRepository<int, Event> _eventRepo;
        private readonly IRepository<int, User> _userRepo;
        private readonly IRepository<int, EventRegistration> _registrationRepo;

        public EventService(
            IRepository<int, Event> eventRepo,
            IRepository<int, User> userRepo,
            IRepository<int, EventRegistration> registrationRepo)
        {
            _eventRepo = eventRepo;
            _userRepo = userRepo;
            _registrationRepo = registrationRepo;
        }

        public async Task<EventResponseDTO> CreateEventAsync(CreateEventRequestDTO dto)
        {
            var user = await _userRepo.GetByIdAsync(dto.CreatedByUserId);

            if (user == null)
                throw new NotFoundException("User not found");

            if (dto.IsPaidEvent && dto.TicketPrice <= 0)
                throw new BadRequestException("Paid event must have valid price");

            if (dto.StartTime != null && dto.EndTime != null && dto.StartTime >= dto.EndTime)
                throw new BadRequestException("EndTime must be after StartTime");

            var newEvent = new Event
            {
                Title = dto.Title,
                Description = dto.Description,
                EventDate = dto.EventDate,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Location = dto.Location,
                Category = dto.Category,
                Visibility = dto.Visibility,
                CreatedByUserId = dto.CreatedByUserId,
                IsPaidEvent = dto.IsPaidEvent,
                TicketPrice = dto.TicketPrice,
                CreatedAt = DateTime.UtcNow,
                Status = EventStatus.ACTIVE,
                ApprovalStatus = dto.Visibility == EventVisibility.PUBLIC
                    ? ApprovalStatus.PENDING
                    : ApprovalStatus.APPROVED
            };

            var created = await _eventRepo.AddAsync(newEvent);

            return MapToDTO(created);
        }

        public async Task<IEnumerable<EventResponseDTO>> GetAllAsync()
        {
            var events = await _eventRepo
                .GetQueryable()
                .Where(e => e.Status == EventStatus.ACTIVE)
                .ToListAsync();

            return events.Select(MapToDTO);
        }

        public async Task<EventResponseDTO?> GetByIdAsync(int id)
        {
            var ev = await _eventRepo.GetByIdAsync(id);

            if (ev == null)
                throw new NotFoundException("Event not found");

            return MapToDTO(ev);
        }

        public async Task<EventResponseDTO?> DeleteAsync(int id)
        {
            var hasRegistrations = await _registrationRepo
                .GetQueryable()
                .AnyAsync(r => r.EventId == id);

            if (hasRegistrations)
                throw new BadRequestException("Cannot delete event because users already registered.");

            var deleted = await _eventRepo.DeleteAsync(id);

            if (deleted == null)
                throw new NotFoundException("Event not found");

            return MapToDTO(deleted);
        }

        public async Task<EventResponseDTO?> ApproveAsync(int eventId, int adminId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);

            if (ev == null)
                throw new NotFoundException("Event not found");

            ev.ApprovalStatus = ApprovalStatus.APPROVED;
            ev.ApprovedByUserId = adminId;

            var updated = await _eventRepo.UpdateAsync(eventId, ev);

            return MapToDTO(updated!);
        }

        public async Task<EventResponseDTO?> RejectAsync(int eventId, int adminId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);

            if (ev == null)
                throw new NotFoundException("Event not found");

            ev.ApprovalStatus = ApprovalStatus.REJECTED;
            ev.ApprovedByUserId = adminId;

            var updated = await _eventRepo.UpdateAsync(eventId, ev);

            return MapToDTO(updated!);
        }

        public async Task<EventResponseDTO?> CancelEventAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);

            if (ev == null)
                throw new NotFoundException("Event not found");

            ev.Status = EventStatus.CANCELLED;

            var updated = await _eventRepo.UpdateAsync(eventId, ev);

            return MapToDTO(updated!);
        }

        public async Task<IEnumerable<EventResponseDTO>> SearchAsync(string keyword)
        {
            var events = await _eventRepo
                .GetQueryable()
                .Where(e =>
                    (e.Title.Contains(keyword) ||
                     e.Description.Contains(keyword) ||
                     e.Location.Contains(keyword))
                    && e.Status == EventStatus.ACTIVE)
                .ToListAsync();

            return events.Select(MapToDTO);
        }

        public async Task<IEnumerable<EventResponseDTO>> GetByDateRangeAsync(DateTime start, DateTime end)
        {
            var events = await _eventRepo
                .GetQueryable()
                .Where(e =>
                    e.EventDate >= start &&
                    e.EventDate <= end &&
                    e.Status == EventStatus.ACTIVE &&
                    e.ApprovalStatus == ApprovalStatus.APPROVED)
                .ToListAsync();

            return events.Select(MapToDTO);
        }

        public async Task<PagedResultDTO<EventResponseDTO>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _eventRepo
                .GetQueryable()
                .Where(e => e.Status == EventStatus.ACTIVE);

            var total = await query.CountAsync();

            var events = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDTO<EventResponseDTO>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = total,
                Data = events.Select(MapToDTO)
            };
        }
        //get events created by organizer
        public async Task<IEnumerable<EventResponseDTO>> GetMyEventsAsync(int userId)
        {
            var events = await _eventRepo
                .GetQueryable()
                .Where(e => e.CreatedByUserId == userId)
                .ToListAsync();

            return events.Select(MapToDTO);
        }

        private EventResponseDTO MapToDTO(Event ev)
        {
            return new EventResponseDTO
            {
                EventId = ev.EventId,
                Title = ev.Title,
                Description = ev.Description,
                EventDate = ev.EventDate,
                Location = ev.Location,
                Category = ev.Category,
                Visibility = ev.Visibility,
                ApprovalStatus = ev.ApprovalStatus,
                IsPaidEvent = ev.IsPaidEvent,
                TicketPrice = ev.TicketPrice
            };
        }
    }
}