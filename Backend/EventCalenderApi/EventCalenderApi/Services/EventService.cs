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
        private readonly IRepository<int, Payment> _paymentRepo;
        private readonly IAuditLogRepository _auditRepo;

        public EventService(
            IRepository<int, Event> eventRepo,
            IRepository<int, User> userRepo,
            IRepository<int, EventRegistration> registrationRepo,
            IRepository<int, Payment> paymentRepo,
            IAuditLogRepository auditRepo)
        {
            _eventRepo = eventRepo;
            _userRepo = userRepo;
            _registrationRepo = registrationRepo;
            _paymentRepo = paymentRepo;
            _auditRepo = auditRepo;
        }

        // ================= CREATE =================
        public async Task<EventResponseDTO> CreateEventAsync(CreateEventRequestDTO dto)
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            if (dto.EventDate < today)
                throw new BadRequestException("Cannot create event in the past");

            if (dto.EventDate == today && dto.StartTime <= now.TimeOfDay)
                throw new BadRequestException("Start time must be in the future");

            if (dto.StartTime != null && dto.EndTime != null && dto.StartTime >= dto.EndTime)
                throw new BadRequestException("EndTime must be after StartTime");

            if (dto.EventEndDate != null && dto.EventEndDate < dto.EventDate)
                throw new BadRequestException("Event end date cannot be before start date");

            if (dto.RegistrationDeadline != null)
            {
                var deadline = dto.RegistrationDeadline.Value;

                if (deadline.Date < today)
                    throw new BadRequestException("Registration deadline date cannot be in the past");

                if (deadline.Date == today && deadline <= now)
                    throw new BadRequestException("Registration deadline time cannot be in the past");

                var eventStartDateTime = dto.EventDate.Add(dto.StartTime ?? TimeSpan.Zero);

                if (deadline >= eventStartDateTime)
                    throw new BadRequestException("Registration deadline must be before event start time");
            }

            var exists = await _eventRepo.GetQueryable()
                .AnyAsync(e =>
                    e.Title.ToLower() == dto.Title.ToLower() &&
                    e.EventDate.Date == dto.EventDate.Date &&
                    e.StartTime == dto.StartTime &&
                    e.CreatedByUserId == dto.CreatedByUserId &&
                    e.Status != EventStatus.CANCELLED);

            if (exists)
                throw new BadRequestException("You already created this event");

            if (dto.IsPaidEvent && dto.TicketPrice <= 0)
                throw new BadRequestException("Ticket price Cannot Be Empty");

            var ev = new Event
            {
                Title = dto.Title,
                Description = dto.Description,
                EventDate = dto.EventDate,
                EventEndDate = dto.EventEndDate,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Location = dto.Location,
                Category = EventCategory.PUBLIC,
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

            return MapToDTO(created, 0);
        }

        // ================= GET ALL =================
        public async Task<IEnumerable<EventResponseDTO>> GetAllAsync()
        {
            var data = await _eventRepo.GetQueryable()
                .Include(e => e.CreatedBy)
                .Where(e => e.Status == EventStatus.ACTIVE &&
                            e.ApprovalStatus == ApprovalStatus.APPROVED)
                .ToListAsync();

            var result = new List<EventResponseDTO>();

            foreach (var ev in data)
            {
                int bookedCount = ev.IsPaidEvent
                    ? await _paymentRepo.GetQueryable().CountAsync(p => p.EventId == ev.EventId && p.Status == PaymentStatus.SUCCESS)
                    : await _registrationRepo.GetQueryable().CountAsync(r => r.EventId == ev.EventId && r.Status == RegistrationStatus.REGISTERED);

                result.Add(MapToDTO(ev, bookedCount));
            }

            return result;
        }

        // ================= GET BY ID =================
        public async Task<EventResponseDTO> GetByIdAsync(int id)
        {
            var ev = await _eventRepo.GetByIdAsync(id)
                ?? throw new NotFoundException("Event not found");

            int bookedCount = ev.IsPaidEvent
                ? await _paymentRepo.GetQueryable().CountAsync(p => p.EventId == ev.EventId && p.Status == PaymentStatus.SUCCESS)
                : await _registrationRepo.GetQueryable().CountAsync(r => r.EventId == ev.EventId && r.Status == RegistrationStatus.REGISTERED);

            return MapToDTO(ev, bookedCount);
        }

        // ================= DELETE =================
        public async Task<EventResponseDTO> DeleteAsync(int id)
        {
            var deleted = await _eventRepo.DeleteAsync(id)
                ?? throw new NotFoundException("Event not found");

            return MapToDTO(deleted, 0);
        }

        // ================= CANCEL =================
        public async Task<EventResponseDTO> CancelEventAsync(int eventId, int userId, string role)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId)
                ?? throw new NotFoundException("Event not found");

            var payments = await _paymentRepo.GetQueryable()
                .Where(p => p.EventId == eventId && p.Status == PaymentStatus.SUCCESS)
                .ToListAsync();

            foreach (var payment in payments)
            {
                payment.Status = PaymentStatus.REFUNDED;
                payment.RefundedAmount = payment.AmountPaid;
                payment.RefundedAt = DateTime.UtcNow;
                payment.CommissionAmount = 0;
                payment.OrganizerAmount = 0;

                await _paymentRepo.UpdateAsync(payment.PaymentId, payment);
            }

            ev.Status = EventStatus.CANCELLED;
            await _eventRepo.UpdateAsync(eventId, ev);

            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = userId,
                Role = role,
                Action = "CANCEL_EVENT",
                Entity = "Event",
                EntityId = eventId
            });

            return MapToDTO(ev, 0);
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

            return MapToDTO(updated!, 0);
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

            return MapToDTO(updated!, 0);
        }

        // ================= SEARCH =================
        public async Task<IEnumerable<EventResponseDTO>> SearchAsync(string keyword)
        {
            var data = await _eventRepo.GetQueryable()
                .Where(e => e.Title.Contains(keyword))
                .ToListAsync();

            return data.Select(e => MapToDTO(e, 0));
        }

        public async Task<IEnumerable<EventResponseDTO>> GetByDateRangeAsync(DateTime start, DateTime end)
        {
            var data = await _eventRepo.GetQueryable()
                .Where(e => e.EventDate >= start && e.EventDate <= end)
                .ToListAsync();

            return data.Select(e => MapToDTO(e, 0));
        }

        public async Task<PagedResultDTO<EventResponseDTO>> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _eventRepo.GetQueryable();

            var total = await query.CountAsync();

            var data = await query.Skip((pageNumber - 1) * pageSize)
                                  .Take(pageSize)
                                  .ToListAsync();

            return new PagedResultDTO<EventResponseDTO>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = total,
                Data = data.Select(e => MapToDTO(e, 0))
            };
        }

        public async Task<IEnumerable<EventResponseDTO>> GetMyEventsAsync(int userId)
        {
            var data = await _eventRepo.GetQueryable()
                .Where(e => e.CreatedByUserId == userId)
                .ToListAsync();

            return data.Select(e => MapToDTO(e, 0));
        }

        public async Task<IEnumerable<EventResponseDTO>> GetRegisteredEventsAsync(int userId)
        {
            var data = await _registrationRepo.GetQueryable()
                .Include(r => r.Event)
                .Where(r => r.UserId == userId && r.Status == RegistrationStatus.REGISTERED)
                .Select(r => r.Event!)
                .ToListAsync();

            return data.Select(e => MapToDTO(e, 0));
        }

        public async Task<RefundSummaryDTO> GetRefundSummaryAsync(int eventId)
        {
            var refunds = await _paymentRepo.GetQueryable()
                .Where(p => p.EventId == eventId && p.Status == PaymentStatus.REFUNDED)
                .ToListAsync();

            return new RefundSummaryDTO
            {
                EventId = eventId,
                TotalUsersRefunded = refunds.Count,
                TotalRefundAmount = refunds.Sum(p => p.RefundedAmount ?? 0)
            };
        }

        public async Task<IEnumerable<EventResponseDTO>> GetPendingEventsAsync()
        {
            var data = await _eventRepo.GetQueryable()
                .Include(e => e.CreatedBy)
                .Where(e => e.ApprovalStatus == ApprovalStatus.PENDING)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            return data.Select(e => MapToDTO(e, 0));
        }

        public async Task<IEnumerable<EventResponseDTO>> GetRejectedEventsAsync()
        {
            var data = await _eventRepo.GetQueryable()
                .Include(e => e.CreatedBy)
                .Where(e => e.ApprovalStatus == ApprovalStatus.REJECTED)
                .ToListAsync();

            return data.Select(e => MapToDTO(e, 0));
        }

        public async Task<IEnumerable<EventResponseDTO>> GetApprovedEventsAsync()
        {
            var data = await _eventRepo.GetQueryable()
                .Include(e => e.CreatedBy)
                .Where(e => e.ApprovalStatus == ApprovalStatus.APPROVED)
                .ToListAsync();

            return data.Select(e => MapToDTO(e, 0));
        }

        // ================= MAPPER =================
        private static EventResponseDTO MapToDTO(Event ev, int bookedCount)
        {
            int seatsLeft = ev.SeatsLimit != null
                ? Math.Max(0, ev.SeatsLimit.Value - bookedCount)
                : -1;

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
                TicketPrice = ev.TicketPrice,
                SeatsLeft = seatsLeft,
                OrganizerName = ev.CreatedBy?.Name ?? string.Empty
            };
        }
    }
}