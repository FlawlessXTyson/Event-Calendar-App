using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Event;
using EventCalenderApi.Interfaces;
using System.Linq;
using EventCalenderApi.Interfaces.ServiceInterfaces;

namespace EventCalenderApi.Services
{
    public class EventService : IEventService
    {
        private readonly IRepository<int, Event> _eventRepo;
        private readonly IRepository<int, User> _userRepo;

        public EventService(IRepository<int, Event> eventRepo,
                            IRepository<int, User> userRepo)
        {
            _eventRepo = eventRepo;
            _userRepo = userRepo;
        }

        public async Task<EventResponseDTO> CreateEventAsync(CreateEventRequestDTO dto)
        {
            var user = await _userRepo.GetByIdAsync(dto.CreatedByUserId);

            if (user == null)
                throw new Exception("User not found");

            if (dto.IsPaidEvent && dto.TicketPrice <= 0)
                throw new Exception("Paid event must have valid price");

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
                SeatsLimit = dto.SeatsLimit,
                RegistrationDeadline = dto.RegistrationDeadline,
                IsPaidEvent = dto.IsPaidEvent,
                TicketPrice = dto.TicketPrice,
                CreatedAt = DateTime.Now,
                ApprovalStatus = dto.Visibility == EventVisibility.PUBLIC
                    ? ApprovalStatus.PENDING
                    : ApprovalStatus.APPROVED
            };

            var created = await _eventRepo.AddAsync(newEvent);

            return MapToDTO(created);
        }

        public async Task<IEnumerable<EventResponseDTO>> GetAllAsync()
        {
            var events = await _eventRepo.GetAllAsync();
            return events.Select(MapToDTO);
        }

        public async Task<EventResponseDTO?> GetByIdAsync(int id)
        {
            var ev = await _eventRepo.GetByIdAsync(id);
            return ev == null ? null : MapToDTO(ev);
        }

        public async Task<EventResponseDTO?> DeleteAsync(int id)
        {
            var deleted = await _eventRepo.DeleteAsync(id);
            return deleted == null ? null : MapToDTO(deleted);
        }

        public async Task<EventResponseDTO?> ApproveAsync(int eventId, int adminId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null) return null;

            ev.ApprovalStatus = ApprovalStatus.APPROVED;
            ev.ApprovedByUserId = adminId;

            var updated = await _eventRepo.UpdateAsync(eventId, ev);
            return MapToDTO(updated!);
        }

        public async Task<EventResponseDTO?> RejectAsync(int eventId, int adminId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null) return null;

            ev.ApprovalStatus = ApprovalStatus.REJECTED;
            ev.ApprovedByUserId = adminId;

            var updated = await _eventRepo.UpdateAsync(eventId, ev);
            return MapToDTO(updated!);
        }
        public async Task<IEnumerable<EventResponseDTO>> SearchAsync(string keyword)
        {
            var events = await _eventRepo.GetAllAsync();

            return events
                .Where(e => e.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .Select(MapToDTO);
        }
        public async Task<IEnumerable<EventResponseDTO>> GetByDateRangeAsync(DateTime start, DateTime end)
        {
            var events = await _eventRepo.GetAllAsync();

            return events
                .Where(e =>
                    e.EventDate.Date >= start.Date &&
                    e.EventDate.Date <= end.Date &&
                    e.ApprovalStatus == ApprovalStatus.APPROVED)
                .Select(MapToDTO);
        }
        public async Task<PagedResultDTO<EventResponseDTO>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var events = await _eventRepo.GetAllAsync();

            var total = events.Count();

            var pagedData = events
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToDTO);

            return new PagedResultDTO<EventResponseDTO>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = total,
                Data = pagedData
            };
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