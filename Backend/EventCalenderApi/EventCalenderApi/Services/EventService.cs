using EventCalenderApi.EventCalenderAppDataLibrary;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Event;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.Enums;
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
        private readonly EventCalendarDbContext _context;

        public EventService(
            IRepository<int, Event> eventRepo,
            IRepository<int, User> userRepo,
            IRepository<int, EventRegistration> registrationRepo,
            EventCalendarDbContext context)
        {
            _eventRepo = eventRepo;
            _userRepo = userRepo;
            _registrationRepo = registrationRepo;
            _context = context;
        }

        // ✅ CREATE EVENT
        public async Task<EventResponseDTO> CreateEventAsync(CreateEventRequestDTO dto)
        {
            if (dto == null)
                throw new BadRequestException("Request body cannot be null");

            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new BadRequestException("Title is required");

            if (dto.EventDate < DateTime.UtcNow.Date)
                throw new BadRequestException("Event date cannot be in the past");

            var user = await _userRepo.GetByIdAsync(dto.CreatedByUserId)
                ?? throw new NotFoundException("User not found");

            var duplicateEvent = await _eventRepo.GetQueryable().AnyAsync(e =>
                e.CreatedByUserId == dto.CreatedByUserId &&
                e.Title.ToLower() == dto.Title.ToLower() &&
                e.EventDate.Date == dto.EventDate.Date &&
                e.StartTime == dto.StartTime
            );

            if (duplicateEvent)
                throw new BadRequestException("You already created this event");

            var newEvent = new Event
            {
                Title = dto.Title,
                Description = dto.Description,
                EventDate = dto.EventDate,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Location = dto.Location,
                Category = dto.Category,
                Visibility = EventVisibility.PUBLIC,
                CreatedByUserId = dto.CreatedByUserId,
                IsPaidEvent = dto.IsPaidEvent,
                TicketPrice = dto.TicketPrice,
                CreatedAt = DateTime.UtcNow,
                Status = EventStatus.ACTIVE,
                ApprovalStatus = ApprovalStatus.PENDING
            };

            var created = await _eventRepo.AddAsync(newEvent);
            return MapToDTO(created);
        }

        // ✅ GET ALL
        public async Task<IEnumerable<EventResponseDTO>> GetAllAsync()
        {
            return await _eventRepo.GetQueryable()
                .Where(e => e.Status == EventStatus.ACTIVE && e.ApprovalStatus == ApprovalStatus.APPROVED)
                .Select(e => MapToDTO(e))
                .ToListAsync();
        }

        // ✅ GET BY ID
        public async Task<EventResponseDTO> GetByIdAsync(int id)
        {
            var ev = await _eventRepo.GetByIdAsync(id)
                ?? throw new NotFoundException("Event not found");

            if (ev.ApprovalStatus != ApprovalStatus.APPROVED)
                throw new NotFoundException("Event not found");

            return MapToDTO(ev);
        }

        // ✅ DELETE
        public async Task<EventResponseDTO> DeleteAsync(int id)
        {
            var hasRegistrations = await _registrationRepo
                .GetQueryable()
                .AnyAsync(r => r.EventId == id && r.Status == RegistrationStatus.REGISTERED);

            if (hasRegistrations)
                throw new BadRequestException("Cannot delete event with registrations. Cancel instead.");

            var deleted = await _eventRepo.DeleteAsync(id)
                ?? throw new NotFoundException("Event not found");

            return MapToDTO(deleted);
        }

        // 🔥 CANCEL EVENT + REFUND ALL USERS
        public async Task<EventResponseDTO> CancelEventAsync(int eventId, int userId, string role)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId)
                ?? throw new NotFoundException("Event not found");

            if (role == "ORGANIZER" && ev.CreatedByUserId != userId)
                throw new UnauthorizedException("You can cancel only your own event");

            if (ev.Status == EventStatus.CANCELLED)
                throw new BadRequestException("Event already cancelled");

            var payments = await _context.Payments
                .Where(p => p.EventId == eventId && p.Status == PaymentStatus.SUCCESS)
                .ToListAsync();

            foreach (var payment in payments)
            {
                payment.Status = PaymentStatus.REFUNDED;
                payment.RefundedAmount = payment.AmountPaid;
                payment.RefundedAt = DateTime.UtcNow;

                payment.CommissionAmount = 0;
                payment.OrganizerAmount = 0;
            }

            ev.Status = EventStatus.CANCELLED;

            await _context.SaveChangesAsync();

            return MapToDTO(ev);
        }

        // ✅ REFUND SUMMARY (YOUR DTO VERSION)
        public async Task<RefundSummaryDTO> GetRefundSummaryAsync(int eventId)
        {
            var refunds = await _context.Payments
                .Where(p => p.EventId == eventId && p.Status == PaymentStatus.REFUNDED)
                .ToListAsync();

            return new RefundSummaryDTO
            {
                EventId = eventId,
                TotalUsersRefunded = refunds.Count,
                TotalRefundAmount = refunds.Sum(p => p.RefundedAmount ?? 0)
            };
        }

        // ✅ APPROVE
        public async Task<EventResponseDTO> ApproveAsync(int eventId, int adminId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId)
                ?? throw new NotFoundException("Event not found");

            ev.ApprovalStatus = ApprovalStatus.APPROVED;
            ev.ApprovedByUserId = adminId;

            var updated = await _eventRepo.UpdateAsync(eventId, ev);
            return MapToDTO(updated!);
        }

        // ✅ REJECT
        public async Task<EventResponseDTO> RejectAsync(int eventId, int adminId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId)
                ?? throw new NotFoundException("Event not found");

            ev.ApprovalStatus = ApprovalStatus.REJECTED;
            ev.ApprovedByUserId = adminId;

            var updated = await _eventRepo.UpdateAsync(eventId, ev);
            return MapToDTO(updated!);
        }

        // ✅ REGISTERED EVENTS
        public async Task<IEnumerable<EventResponseDTO>> GetRegisteredEventsAsync(int userId)
        {
            var events = await _registrationRepo
                .GetQueryable()
                .Include(r => r.Event)
                .Where(r => r.UserId == userId && r.Status == RegistrationStatus.REGISTERED)
                .Select(r => r.Event!)
                .Where(e => e.Status == EventStatus.ACTIVE && e.ApprovalStatus == ApprovalStatus.APPROVED)
                .ToListAsync();

            if (!events.Any())
                throw new NotFoundException("No events found");

            return events.Select(MapToDTO);
        }

        // ✅ SEARCH
        public async Task<IEnumerable<EventResponseDTO>> SearchAsync(string keyword)
        {
            return await _eventRepo.GetQueryable()
                .Where(e =>
                    (e.Title.Contains(keyword) || e.Location.Contains(keyword)) &&
                    e.Status == EventStatus.ACTIVE &&
                    e.ApprovalStatus == ApprovalStatus.APPROVED)
                .Select(e => MapToDTO(e))
                .ToListAsync();
        }

        // ✅ DATE RANGE
        public async Task<IEnumerable<EventResponseDTO>> GetByDateRangeAsync(DateTime start, DateTime end)
        {
            var events = await _eventRepo.GetQueryable()
                .Where(e =>
                    e.EventDate >= start &&
                    e.EventDate <= end &&
                    e.Status == EventStatus.ACTIVE &&
                    e.ApprovalStatus == ApprovalStatus.APPROVED)
                .ToListAsync();

            if (!events.Any())
                throw new NotFoundException("No events found");

            return events.Select(MapToDTO);
        }

        // ✅ PAGINATION
        public async Task<PagedResultDTO<EventResponseDTO>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _eventRepo.GetQueryable()
                .Where(e => e.Status == EventStatus.ACTIVE && e.ApprovalStatus == ApprovalStatus.APPROVED);

            var total = await query.CountAsync();

            var data = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResultDTO<EventResponseDTO>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = total,
                Data = data.Select(MapToDTO)
            };
        }

        // ✅ MY EVENTS
        public async Task<IEnumerable<EventResponseDTO>> GetMyEventsAsync(int userId)
        {
            return await _eventRepo.GetQueryable()
                .Where(e => e.CreatedByUserId == userId)
                .Select(e => MapToDTO(e))
                .ToListAsync();
        }

        private static EventResponseDTO MapToDTO(Event ev)
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