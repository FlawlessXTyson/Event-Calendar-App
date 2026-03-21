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
        private readonly IAuditLogRepository _auditRepo;

        public EventService(
            IRepository<int, Event> eventRepo,
            IRepository<int, User> userRepo,
            IRepository<int, EventRegistration> registrationRepo,
            EventCalendarDbContext context,
            IAuditLogRepository auditRepo)
        {
            _eventRepo = eventRepo;
            _userRepo = userRepo;
            _registrationRepo = registrationRepo;
            _context = context;
            _auditRepo = auditRepo;
        }

        // ================= CREATE =================
        public async Task<EventResponseDTO> CreateEventAsync(CreateEventRequestDTO dto)
        {
            var ev = new Event
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
                SeatsLimit = dto.SeatsLimit,
                RegistrationDeadline = dto.RegistrationDeadline,
                IsPaidEvent = dto.IsPaidEvent,
                TicketPrice = dto.IsPaidEvent ? dto.TicketPrice : 0,
                CreatedAt = DateTime.UtcNow,
                Status = EventStatus.ACTIVE,
                ApprovalStatus = ApprovalStatus.PENDING
            };

            var created = await _eventRepo.AddAsync(ev);

            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = dto.CreatedByUserId,
                Role = "ORGANIZER",
                Action = "CREATE_EVENT",
                Entity = "Event",
                EntityId = created.EventId
            });

            return MapToDTO(created);
        }

        // ================= GET ALL =================
        public async Task<IEnumerable<EventResponseDTO>> GetAllAsync()
        {
            var data = await _eventRepo.GetQueryable()
                .Where(e => e.Status == EventStatus.ACTIVE &&
                            e.ApprovalStatus == ApprovalStatus.APPROVED)
                .ToListAsync();

            return data.Select(MapToDTO);
        }

        // ================= GET BY ID =================
        public async Task<EventResponseDTO> GetByIdAsync(int id)
        {
            var ev = await _eventRepo.GetByIdAsync(id)
                ?? throw new NotFoundException("Event not found");

            return MapToDTO(ev);
        }

        // ================= DELETE =================
        public async Task<EventResponseDTO> DeleteAsync(int id)
        {
            var deleted = await _eventRepo.DeleteAsync(id)
                ?? throw new NotFoundException("Event not found");

            return MapToDTO(deleted);
        }

        // ================= APPROVE =================
        public async Task<EventResponseDTO> ApproveAsync(int eventId, int adminId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId)
                ?? throw new NotFoundException("Event not found");

            ev.ApprovalStatus = ApprovalStatus.APPROVED;
            ev.ApprovedByUserId = adminId;

            var updated = await _eventRepo.UpdateAsync(eventId, ev);

            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = adminId,
                Role = "ADMIN",
                Action = "APPROVE_EVENT",
                Entity = "Event",
                EntityId = eventId
            });

            return MapToDTO(updated!);
        }

        // ================= REJECT =================
        public async Task<EventResponseDTO> RejectAsync(int eventId, int adminId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId)
                ?? throw new NotFoundException("Event not found");

            ev.ApprovalStatus = ApprovalStatus.REJECTED;
            ev.ApprovedByUserId = adminId;

            var updated = await _eventRepo.UpdateAsync(eventId, ev);

            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = adminId,
                Role = "ADMIN",
                Action = "REJECT_EVENT",
                Entity = "Event",
                EntityId = eventId
            });

            return MapToDTO(updated!);
        }

        // ================= CANCEL =================
        public async Task<EventResponseDTO> CancelEventAsync(int eventId, int userId, string role)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId)
                ?? throw new NotFoundException("Event not found");

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

            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = userId,
                Role = role,
                Action = "CANCEL_EVENT",
                Entity = "Event",
                EntityId = eventId
            });

            return MapToDTO(ev);
        }

        // ================= SEARCH =================
        public async Task<IEnumerable<EventResponseDTO>> SearchAsync(string keyword)
        {
            var data = await _eventRepo.GetQueryable()
                .Where(e => e.Title.Contains(keyword))
                .ToListAsync();

            return data.Select(MapToDTO);
        }

        // ================= DATE RANGE =================
        public async Task<IEnumerable<EventResponseDTO>> GetByDateRangeAsync(DateTime start, DateTime end)
        {
            var data = await _eventRepo.GetQueryable()
                .Where(e => e.EventDate >= start && e.EventDate <= end)
                .ToListAsync();

            return data.Select(MapToDTO);
        }

        // ================= PAGINATION =================
        public async Task<PagedResultDTO<EventResponseDTO>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _eventRepo.GetQueryable();

            var total = await query.CountAsync();

            var data = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDTO<EventResponseDTO>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = total,
                Data = data.Select(MapToDTO)
            };
        }

        // ================= MY EVENTS =================
        public async Task<IEnumerable<EventResponseDTO>> GetMyEventsAsync(int userId)
        {
            var data = await _eventRepo.GetQueryable()
                .Where(e => e.CreatedByUserId == userId)
                .ToListAsync();

            return data.Select(MapToDTO);
        }

        // ================= REGISTERED EVENTS =================
        public async Task<IEnumerable<EventResponseDTO>> GetRegisteredEventsAsync(int userId)
        {
            var data = await _registrationRepo.GetQueryable()
                .Include(r => r.Event)
                .Where(r => r.UserId == userId && r.Status == RegistrationStatus.REGISTERED)
                .Select(r => r.Event!)
                .ToListAsync();

            return data.Select(MapToDTO);
        }

        // ================= REFUND SUMMARY =================
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

        // ================= ADMIN =================
        public async Task<IEnumerable<EventResponseDTO>> GetPendingEventsAsync()
        {
            var data = await _eventRepo.GetQueryable()
                .Where(e => e.ApprovalStatus == ApprovalStatus.PENDING)
                .ToListAsync();

            return data.Select(MapToDTO);
        }

        public async Task<IEnumerable<EventResponseDTO>> GetRejectedEventsAsync()
        {
            var data = await _eventRepo.GetQueryable()
                .Where(e => e.ApprovalStatus == ApprovalStatus.REJECTED)
                .ToListAsync();

            return data.Select(MapToDTO);
        }

        public async Task<IEnumerable<EventResponseDTO>> GetApprovedEventsAsync()
        {
            var data = await _eventRepo.GetQueryable()
                .Where(e => e.ApprovalStatus == ApprovalStatus.APPROVED)
                .ToListAsync();

            return data.Select(MapToDTO);
        }

        // ================= MAPPER =================
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