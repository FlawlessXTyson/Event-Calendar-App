﻿using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Event;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.Enums;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.EntityFrameworkCore;
using EventCalenderApi.Helpers;

namespace EventCalenderApi.Services
{
    public class EventService : IEventService
    {
        private readonly IRepository<int, Event> _eventRepo;
        private readonly IRepository<int, User> _userRepo;
        private readonly IRepository<int, EventRegistration> _registrationRepo;
        private readonly IRepository<int, Payment> _paymentRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly IWalletService _walletSvc;

        public EventService(
            IRepository<int, Event> eventRepo,
            IRepository<int, User> userRepo,
            IRepository<int, EventRegistration> registrationRepo,
            IRepository<int, Payment> paymentRepo,
            IAuditLogRepository auditRepo,
            IWalletService walletSvc)
        {
            _eventRepo = eventRepo;
            _userRepo = userRepo;
            _registrationRepo = registrationRepo;
            _paymentRepo = paymentRepo;
            _auditRepo = auditRepo;
            _walletSvc = walletSvc;
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

        // ================= CANCEL EVENT (ADMIN or ORGANIZER) =================
        public async Task<EventResponseDTO> CancelEventAsync(int eventId, int userId, string role)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId)
                ?? throw new NotFoundException("Event not found");

            var eventStart = ev.EventDate.Add(ev.StartTime ?? TimeSpan.Zero);
            if (IstClock.Now >= eventStart)
                throw new BadRequestException("Cannot cancel an event that has already started");

            var hoursBeforeStart = (eventStart - IstClock.Now).TotalHours;
            bool isLessThan48h   = hoursBeforeStart < 48;

            var payments = await _paymentRepo.GetQueryable()
                .Where(p => p.EventId == eventId && p.Status == PaymentStatus.SUCCESS)
                .ToListAsync();

            int adminId = ev.ApprovedByUserId ?? 0;

            if (role == "ADMIN")
            {
                foreach (var payment in payments)
                {
                    float refundAmount = payment.AmountPaid;
                    float commission   = payment.CommissionAmount;
                    float orgEarning   = payment.OrganizerAmount;

                    payment.Status           = PaymentStatus.REFUNDED;
                    payment.RefundedAmount   = refundAmount;
                    payment.RefundedAt       = DateTime.UtcNow;
                    payment.CommissionAmount = 0;
                    payment.OrganizerAmount  = 0;
                    payment.CancelledBy      = "ADMIN";
                    await _paymentRepo.UpdateAsync(payment.PaymentId, payment);

                    if (adminId > 0 && commission > 0)
                        await _walletSvc.DebitAsync(adminId, commission, "REFUND",
                            $"Admin cancel refund (commission) for event: {ev.Title}");
                    if (orgEarning > 0)
                        await _walletSvc.DebitAsync(ev.CreatedByUserId, orgEarning, "REFUND",
                            $"Admin cancel refund (earnings) for event: {ev.Title}");
                    await _walletSvc.CreditAsync(payment.UserId, refundAmount, "REFUND",
                        $"Full refund — admin cancelled event: {ev.Title}");

                    await _auditRepo.AddAsync(new AuditLog
                    {
                        UserId = userId, Role = role,
                        Action = "ADMIN_CANCEL_REFUND", Entity = "Payment", EntityId = payment.PaymentId
                    });
                }

                if (isLessThan48h && ev.IsPaidEvent && ev.TicketPrice > 0 && payments.Count > 0)
                {
                    float compensation = ev.TicketPrice * 0.5f * payments.Count;
                    if (adminId > 0)
                    {
                        await _walletSvc.DebitAsync(adminId, compensation, "COMPENSATION",
                            $"Organizer compensation (late cancel): {ev.Title}");
                        await _walletSvc.CreditAsync(ev.CreatedByUserId, compensation, "COMPENSATION",
                            $"Compensation (50%/ticket) — admin cancelled: {ev.Title}");
                    }
                }
            }
            else if (role == "ORGANIZER")
            {
                if (ev.CreatedByUserId != userId)
                    throw new UnauthorizedException("You can only cancel your own events");

                foreach (var payment in payments)
                {
                    float refundAmount = payment.AmountPaid;
                    float commission   = payment.CommissionAmount;
                    float orgEarning   = payment.OrganizerAmount;

                    payment.Status           = PaymentStatus.REFUNDED;
                    payment.RefundedAmount   = refundAmount;
                    payment.RefundedAt       = DateTime.UtcNow;
                    payment.CommissionAmount = 0;
                    payment.OrganizerAmount  = 0;
                    payment.CancelledBy      = "ORGANIZER";
                    await _paymentRepo.UpdateAsync(payment.PaymentId, payment);

                    if (isLessThan48h)
                    {
                        float adminContrib = commission * 0.02f;
                        float orgPays      = refundAmount - adminContrib;
                        if (adminId > 0 && adminContrib > 0)
                            await _walletSvc.DebitAsync(adminId, adminContrib, "REFUND",
                                $"2% commission refund — organizer cancelled: {ev.Title}");
                        if (orgPays > 0)
                            await _walletSvc.DebitAsync(ev.CreatedByUserId, orgPays, "REFUND",
                                $"Penalty refund (late cancel): {ev.Title}");
                    }
                    else
                    {
                        if (adminId > 0 && commission > 0)
                            await _walletSvc.DebitAsync(adminId, commission, "REFUND",
                                $"Commission refund — organizer cancelled: {ev.Title}");
                        if (orgEarning > 0)
                            await _walletSvc.DebitAsync(ev.CreatedByUserId, orgEarning, "REFUND",
                                $"Earnings refund — organizer cancelled: {ev.Title}");
                    }

                    await _walletSvc.CreditAsync(payment.UserId, refundAmount, "REFUND",
                        $"Full refund — organizer cancelled event: {ev.Title}");

                    await _auditRepo.AddAsync(new AuditLog
                    {
                        UserId = userId, Role = role,
                        Action = isLessThan48h ? "ORGANIZER_CANCEL_PENALTY_REFUND" : "ORGANIZER_CANCEL_REFUND",
                        Entity = "Payment", EntityId = payment.PaymentId
                    });
                }
            }

            var registrations = await _registrationRepo.GetQueryable()
                .Where(r => r.EventId == eventId && r.Status == RegistrationStatus.REGISTERED)
                .ToListAsync();
            foreach (var reg in registrations)
            {
                reg.Status = RegistrationStatus.CANCELLED;
                await _registrationRepo.UpdateAsync(reg.RegistrationId, reg);
            }

            ev.Status = EventStatus.CANCELLED;
            await _eventRepo.UpdateAsync(eventId, ev);

            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = userId, Role = role,
                Action = "CANCEL_EVENT", Entity = "Event", EntityId = eventId
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

        public async Task<PagedResultDTO<EventResponseDTO>> GetMyEventsPagedAsync(int userId, int pageNumber, int pageSize, DateTime? filterDate)
        {
            var query = _eventRepo.GetQueryable()
                .Where(e => e.CreatedByUserId == userId);

            // Filter by date if provided
            if (filterDate.HasValue)
                query = query.Where(e => e.EventDate.Date == filterDate.Value.Date);

            var total = await query.CountAsync();

            // Sort descending by date + start time
            var data = await query
                .OrderByDescending(e => e.EventDate)
                .ThenByDescending(e => e.StartTime)
                .Skip((pageNumber - 1) * pageSize)
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
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
            // Filter upcoming only in memory (EF can't translate DateTime.Add to SQL)
            var now = IstClock.Now;
            return data.Where(e => e.EventDate.Add(e.StartTime ?? TimeSpan.Zero) > now)
                       .Select(e => MapToDTO(e, 0));
        }

        public async Task<PagedResultDTO<EventResponseDTO>> GetPendingEventsPagedAsync(int pageNumber, int pageSize)
        {
            // Load all pending first, filter upcoming in memory, then paginate
            var all = await _eventRepo.GetQueryable()
                .Include(e => e.CreatedBy)
                .Where(e => e.ApprovalStatus == ApprovalStatus.PENDING)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();

            var now = IstClock.Now;
            var upcoming = all.Where(e => e.EventDate.Add(e.StartTime ?? TimeSpan.Zero) > now).ToList();

            var total = upcoming.Count;
            var data  = upcoming.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return new PagedResultDTO<EventResponseDTO>
            {
                PageNumber   = pageNumber,
                PageSize     = pageSize,
                TotalRecords = total,
                Data         = data.Select(e => MapToDTO(e, 0))
            };
        }

        public async Task<IEnumerable<EventResponseDTO>> GetRejectedEventsAsync()
        {
            var data = await _eventRepo.GetQueryable()
                .Include(e => e.CreatedBy)
                .Where(e => e.ApprovalStatus == ApprovalStatus.REJECTED)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
            return data.Select(e => MapToDTO(e, 0));
        }

        public async Task<PagedResultDTO<EventResponseDTO>> GetRejectedEventsPagedAsync(int pageNumber, int pageSize)
        {
            var query = _eventRepo.GetQueryable()
                .Include(e => e.CreatedBy)
                .Where(e => e.ApprovalStatus == ApprovalStatus.REJECTED)
                .OrderByDescending(e => e.EventDate);
            var total = await query.CountAsync();
            var data  = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResultDTO<EventResponseDTO> { PageNumber = pageNumber, PageSize = pageSize, TotalRecords = total, Data = data.Select(e => MapToDTO(e, 0)) };
        }

        public async Task<IEnumerable<EventResponseDTO>> GetApprovedEventsAsync()
        {
            var data = await _eventRepo.GetQueryable()
                .Include(e => e.CreatedBy)
                .Where(e => e.ApprovalStatus == ApprovalStatus.APPROVED)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
            return data.Select(e => MapToDTO(e, 0));
        }

        public async Task<PagedResultDTO<EventResponseDTO>> GetApprovedEventsPagedAsync(int pageNumber, int pageSize)
        {
            var query = _eventRepo.GetQueryable()
                .Include(e => e.CreatedBy)
                .Where(e => e.ApprovalStatus == ApprovalStatus.APPROVED)
                .OrderByDescending(e => e.EventDate);
            var total = await query.CountAsync();
            var data  = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResultDTO<EventResponseDTO> { PageNumber = pageNumber, PageSize = pageSize, TotalRecords = total, Data = data.Select(e => MapToDTO(e, 0)) };
        }

        // ================= EXPIRED EVENTS =================
        public async Task<IEnumerable<EventResponseDTO>> GetExpiredEventsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var data = await _eventRepo.GetQueryable()
                .Include(e => e.CreatedBy)
                .Where(e => (e.EventEndDate != null ? e.EventEndDate.Value.Date : e.EventDate.Date) < today)
                .OrderByDescending(e => e.EventDate)
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

        public async Task<PagedResultDTO<EventResponseDTO>> GetExpiredEventsPagedAsync(int pageNumber, int pageSize)
        {
            var today = DateTime.UtcNow.Date;
            var query = _eventRepo.GetQueryable()
                .Include(e => e.CreatedBy)
                .Where(e => (e.EventEndDate != null ? e.EventEndDate.Value.Date : e.EventDate.Date) < today)
                .OrderByDescending(e => e.EventDate);
            var total = await query.CountAsync();
            var data  = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            var result = new List<EventResponseDTO>();
            foreach (var ev in data)
            {
                int bookedCount = ev.IsPaidEvent
                    ? await _paymentRepo.GetQueryable().CountAsync(p => p.EventId == ev.EventId && p.Status == PaymentStatus.SUCCESS)
                    : await _registrationRepo.GetQueryable().CountAsync(r => r.EventId == ev.EventId && r.Status == RegistrationStatus.REGISTERED);
                result.Add(MapToDTO(ev, bookedCount));
            }
            return new PagedResultDTO<EventResponseDTO> { PageNumber = pageNumber, PageSize = pageSize, TotalRecords = total, Data = result };
        }

        // ================= CANCELLED EVENTS PAGED =================
        public async Task<PagedResultDTO<EventResponseDTO>> GetCancelledEventsPagedAsync(int pageNumber, int pageSize)
        {
            var query = _eventRepo.GetQueryable()
                .Include(e => e.CreatedBy)
                .Where(e => e.Status == EventStatus.CANCELLED)
                .OrderByDescending(e => e.EventDate);

            var total = await query.CountAsync();
            var data  = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResultDTO<EventResponseDTO>
            {
                PageNumber   = pageNumber,
                PageSize     = pageSize,
                TotalRecords = total,
                Data         = data.Select(e => MapToDTO(e, 0))
            };
        }

        // ================= ALL EVENTS PAGED (ADMIN) =================
        public async Task<PagedResultDTO<EventResponseDTO>> GetAllEventsPagedAsync(int pageNumber, int pageSize, string? search)
        {
            var query = _eventRepo.GetQueryable()
                .Include(e => e.CreatedBy)
                .OrderByDescending(e => e.EventDate)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(e => e.Title.Contains(search) ||
                                         (e.Location != null && e.Location.Contains(search)));

            var total = await query.CountAsync();
            var data  = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResultDTO<EventResponseDTO>
            {
                PageNumber   = pageNumber,
                PageSize     = pageSize,
                TotalRecords = total,
                Data         = data.Select(e => MapToDTO(e, 0))
            };
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
                OrganizerName = ev.CreatedBy?.Name ?? string.Empty,
                RefundCutoffDays = ev.RefundCutoffDays,
                EarlyRefundPercentage = ev.EarlyRefundPercentage,
                RegistrationDeadline = ev.RegistrationDeadline,
                StartTime = ev.StartTime.HasValue
                    ? $"{(int)ev.StartTime.Value.TotalHours:D2}:{ev.StartTime.Value.Minutes:D2}"
                    : null,
                EndTime = ev.EndTime.HasValue
                    ? $"{(int)ev.EndTime.Value.TotalHours:D2}:{ev.EndTime.Value.Minutes:D2}"
                    : null,
                IsRegistrationOpen = ComputeIsRegistrationOpen(ev),
                HasStarted = IstClock.Now >= ev.EventDate.Add(ev.StartTime ?? TimeSpan.Zero),
                HasEnded   = IstClock.Now >  (ev.EventEndDate ?? ev.EventDate).Add(ev.EndTime ?? new TimeSpan(23, 59, 59))
            };
        }

        /// <summary>
        /// Mirrors the exact validation logic in EventRegistrationService.RegisterAsync
        /// so the frontend can show "Registration Closed" before the user even tries.
        /// All comparisons use UTC — same as the registration validator.
        /// </summary>
        private static bool ComputeIsRegistrationOpen(Event ev)
        {
            var nowIst = IstClock.Now;    // IST — for event start/end (stored as IST)
            var nowUtc = IstClock.UtcNow; // UTC — for registration deadline (stored as UTC)

            // Registration deadline stored as UTC (frontend sends ISO string)
            if (ev.RegistrationDeadline != null && ev.RegistrationDeadline < nowUtc)
                return false;

            // Event already started (EventDate + StartTime stored as IST)
            var eventStart = ev.EventDate.Add(ev.StartTime ?? TimeSpan.Zero);
            if (nowIst >= eventStart)
                return false;

            // Event already ended
            var eventEndDate = ev.EventEndDate ?? ev.EventDate;
            var eventEndTime = ev.EndTime ?? new TimeSpan(23, 59, 59);
            var eventEnd = eventEndDate.Add(eventEndTime);
            if (nowIst > eventEnd)
                return false;

            return true;
        }
    }
}