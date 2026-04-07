using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.Helpers;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.EventRegisration;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.Enums;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.EntityFrameworkCore;

namespace EventCalenderApi.Services
{
    public class EventRegistrationService : IEventRegistrationService
    {
        private readonly IRepository<int, EventRegistration> _registrationRepo;
        private readonly IRepository<int, Event> _eventRepo;
        private readonly IRepository<int, Payment> _paymentRepo;
        private readonly IRepository<int, RefundRequest> _refundRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly IWalletService _walletSvc;
        private readonly INotificationService _notifSvc;
        private readonly IRepository<int, User> _userRepo;

        public EventRegistrationService(
            IRepository<int, EventRegistration> registrationRepo,
            IRepository<int, Event> eventRepo,
            IRepository<int, Payment> paymentRepo,
            IRepository<int, RefundRequest> refundRepo,
            IAuditLogRepository auditRepo,
            IWalletService walletSvc,
            INotificationService notifSvc,
            IRepository<int, User> userRepo)
        {
            _registrationRepo = registrationRepo;
            _eventRepo = eventRepo;
            _paymentRepo = paymentRepo;
            _refundRepo = refundRepo;
            _auditRepo = auditRepo;
            _walletSvc = walletSvc;
            _notifSvc = notifSvc;
            _userRepo = userRepo;
        }


        /// <summary>
        /// Registers the specified user for an event if all registration conditions are met.
        /// </summary>
        /// <param name="dto">An object containing the event registration request details, including the event identifier.</param>
        /// <param name="userId">The unique identifier of the user attempting to register for the event.</param>
        /// <returns>An object containing the details of the successful event registration.</returns>
        /// <exception cref="NotFoundException">Thrown if the event specified in <paramref name="dto"/> does not exist.</exception>
        /// <exception cref="BadRequestException">Thrown if the event is not approved, not active, the registration deadline has passed, the event has already
        /// started or ended, no seats are available, or the user has already registered for the event.</exception>
        // REGISTER 
        public async Task<EventRegistrationResponseDTO> RegisterAsync(EventRegisterationRequestDTO dto, int userId)
        {
            var ev = await _eventRepo.GetByIdAsync(dto.EventId)
                ?? throw new NotFoundException("Event not found");

            if (ev.ApprovalStatus != ApprovalStatus.APPROVED)
                throw new BadRequestException("Event is not approved");

            if (ev.Status != EventStatus.ACTIVE)
                throw new BadRequestException("Event is not active");

            if (ev.RegistrationDeadline != null && ev.RegistrationDeadline < DateTime.UtcNow)
                throw new BadRequestException("Registration deadline has passed");



            // ================= TIME VALIDATION =================
            
            var now = IstClock.Now; // IST — EventDate is stored in IST

            // START CHECK (UNCHANGED)
            var eventStartDateTime = ev.EventDate.Add(ev.StartTime ?? TimeSpan.Zero);

            if (now >= eventStartDateTime)
                throw new BadRequestException("Event already started");

            //  IMPROVED END CHECK (SAFE FIX - supports multi-day properly)
            var eventEndDate = ev.EventEndDate ?? ev.EventDate;
            var eventEndTime = ev.EndTime ?? new TimeSpan(23, 59, 59);
            var eventEndDateTime = eventEndDate.Add(eventEndTime);

            if (now > eventEndDateTime)
                throw new BadRequestException("Event has already ended");


            
            // ================= SEAT LOGIC =================
            if (ev.SeatsLimit != null)
            {
                var totalRegistered = await _registrationRepo
                    .GetQueryable()
                    .CountAsync(r =>
                        r.EventId == dto.EventId &&
                        r.Status == RegistrationStatus.REGISTERED);

                if (totalRegistered >= ev.SeatsLimit)
                    throw new BadRequestException("No seats available");
            }

            // ================= DUPLICATE CHECK =================
            var exists = await _registrationRepo
                .GetQueryable()
                .AnyAsync(r =>
                    r.EventId == dto.EventId &&
                    r.UserId == userId &&
                    r.Status == RegistrationStatus.REGISTERED);

            if (exists)
                throw new BadRequestException("You have already registered for this event");

            // ================= CREATE =================
            var registration = new EventRegistration
            {
                EventId = dto.EventId,
                UserId = userId,
                RegisteredAt = DateTime.UtcNow,
                Status = RegistrationStatus.REGISTERED
            };

            var created = await _registrationRepo.AddAsync(registration);

            // ================= AUDIT LOG =================
            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = userId,
                Role = "USER",
                Action = "REGISTER_EVENT",
                Entity = "EventRegistration",
                EntityId = created.RegistrationId
            });

            // ── Notify ORGANIZER for free event registration ──────────────
            if (!ev.IsPaidEvent)
            {
                var user = await _userRepo.GetByIdAsync(userId);
                string userName = user?.Name ?? "A user";
                await _notifSvc.CreateNotificationAsync(
                    ev.CreatedByUserId,
                    "New Registration",
                    $"{userName} registered for your free event '{ev.Title}'.",
                    NotificationType.INFO);
            }

            return MapToDTO(created);
        }

        // ================= CANCEL =================
        public async Task<EventRegistrationResponseDTO> CancelAsync(int registrationId, int userId, string role)
        {
            var registration = await _registrationRepo.GetByIdAsync(registrationId)
                ?? throw new NotFoundException("Registration not found");

            if (registration.Status == RegistrationStatus.CANCELLED)
                throw new BadRequestException("Registration already cancelled");

            if (role == "USER" && registration.UserId != userId)
                throw new UnauthorizedException("You can cancel only your own registration");

            var ev = await _eventRepo.GetByIdAsync(registration.EventId)
                ?? throw new NotFoundException("Event not found");

            //  IMPROVED CANCEL VALIDATION (multi-day support)
            var eventEndDate = ev.EventEndDate ?? ev.EventDate;
            var eventEndTime = ev.EndTime ?? new TimeSpan(23, 59, 59);
            var eventEndDateTime = eventEndDate.Add(eventEndTime);

            if (IstClock.Now > eventEndDateTime)
                throw new BadRequestException("Cannot cancel after event has ended");

            var payment = await _paymentRepo
                .GetQueryable()
                .FirstOrDefaultAsync(p =>
                    p.UserId == registration.UserId &&
                    p.EventId == registration.EventId &&
                    p.Status == PaymentStatus.SUCCESS);

            if (payment != null)
            {
                if (role == "USER")
                {
                    // ── AUTO REFUND based on hours before event start (USER ONLY - DO NOT MODIFY) ──
                    var eventStart = ev.EventDate.Add(ev.StartTime ?? TimeSpan.Zero);
                    var hoursBeforeEvent = (eventStart - IstClock.Now).TotalHours;

                    float refundPct = hoursBeforeEvent switch
                    {
                        >= 48 => 100f,
                        >= 24 => 75f,
                        >= 12 => 50f,
                        > 0   => 25f,
                        _     => 0f
                    };

                    float refundAmount = payment.AmountPaid * refundPct / 100f;
                    float adminRefund  = payment.AmountPaid > 0 ? refundAmount * (payment.CommissionAmount / payment.AmountPaid) : 0;
                    float orgRefund    = refundAmount - adminRefund;

                    payment.Status           = PaymentStatus.REFUNDED;
                    payment.RefundedAmount   = refundAmount;
                    payment.RefundedAt       = DateTime.UtcNow;
                    payment.CommissionAmount = Math.Max(0, payment.CommissionAmount - adminRefund);
                    payment.OrganizerAmount  = Math.Max(0, payment.OrganizerAmount  - orgRefund);
                    payment.CancelledBy      = "USER";

                    await _paymentRepo.UpdateAsync(payment.PaymentId, payment);

                    // ── Credit refund to USER wallet ──────────────────────
                    if (refundAmount > 0)
                    {
                        await _walletSvc.CreditAsync(
                            registration.UserId,
                            refundAmount,
                            "REFUND",
                            $"Refund ({refundPct}%) for cancelling: {ev.Title}");
                    }

                    await _auditRepo.AddAsync(new AuditLog
                    {
                        UserId = userId, Role = role,
                        Action = "REFUND", Entity = "Payment", EntityId = payment.PaymentId
                    });

                    // ── Notify USER about their refund ────────────────────
                    string refundMsg = refundAmount > 0
                        ? $"You cancelled your registration for '{ev.Title}'. " +
                          $"Refund of ₹{refundAmount:F2} ({refundPct}%) has been credited to your wallet."
                        : $"You cancelled your registration for '{ev.Title}'. No refund was applicable (within cutoff window).";

                    await _notifSvc.CreateNotificationAsync(
                        registration.UserId,
                        "Registration Cancelled",
                        refundMsg,
                        refundAmount > 0 ? NotificationType.REFUND : NotificationType.INFO);

                    // ── Notify ORGANIZER about user cancellation ──────────
                    await _notifSvc.CreateNotificationAsync(
                        ev.CreatedByUserId,
                        "User Cancelled Registration",
                        $"A user cancelled their registration for your event '{ev.Title}'. " +
                        $"Refund of ₹{refundAmount:F2} ({refundPct}%) was processed.",
                        NotificationType.INFO);

                    // ── Notify ADMIN if late cancellation (< 12h) ─────────
                    if (hoursBeforeEvent < 12 && ev.ApprovedByUserId.HasValue)
                    {
                        await _notifSvc.CreateNotificationAsync(
                            ev.ApprovedByUserId.Value,
                            "Late User Cancellation",
                            $"A user cancelled their registration for '{ev.Title}' with {hoursBeforeEvent:F1}h remaining. " +
                            $"Refund: ₹{refundAmount:F2} ({refundPct}%).",
                            NotificationType.WARNING);
                    }
                }
                // NOTE: Admin/Organizer event-level cancellation is handled by EventService.CancelEventAsync
                // This path (CancelAsync) is for individual USER registration cancellation only
            }

            registration.Status = RegistrationStatus.CANCELLED;

            var updated = await _registrationRepo.UpdateAsync(registrationId, registration);

            // AUDIT LOG
            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = userId,
                Role = role,
                Action = "CANCEL_REGISTRATION",
                Entity = "EventRegistration",
                EntityId = registrationId
            });

            return MapToDTO(updated!);
        }

        // ================= GET BY EVENT =================
        public async Task<IEnumerable<EventRegistrationResponseDTO>> GetByEventAsync(int eventId)
        {
            var data = await _registrationRepo
                .GetQueryable()
                .Include(r => r.User)
                .Where(r => r.EventId == eventId)
                .ToListAsync();

            return data.Select(MapToDTO);
        }

        // ================= GET BY EVENT PAGED =================
        public async Task<PagedResultDTO<EventRegistrationResponseDTO>> GetByEventPagedAsync(int eventId, int pageNumber, int pageSize, DateTime? filterDate)
        {
            var query = _registrationRepo
                .GetQueryable()
                .Include(r => r.User)
                .Where(r => r.EventId == eventId);

            if (filterDate.HasValue)
            {
                var date = filterDate.Value.Date;
                query = query.Where(r => r.RegisteredAt.Date == date);
            }

            var total = await query.CountAsync();
            var data  = await query
                .OrderByDescending(r => r.RegisteredAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDTO<EventRegistrationResponseDTO>
            {
                PageNumber   = pageNumber,
                PageSize     = pageSize,
                TotalRecords = total,
                Data         = data.Select(MapToDTO)
            };
        }

        // ================= GET MY REGISTRATIONS =================
        public async Task<IEnumerable<EventRegistrationResponseDTO>> GetMyRegistrationsAsync(int userId)
        {
            var data = await _registrationRepo
                .GetQueryable()
                .Where(r =>
                    r.UserId == userId &&
                    r.Status == RegistrationStatus.REGISTERED)
                .ToListAsync();

            return data.Select(MapToDTO);
        }

        // ================= MAPPER =================
        private static EventRegistrationResponseDTO MapToDTO(EventRegistration r)
        {
            return new EventRegistrationResponseDTO
            {
                RegistrationId = r.RegistrationId,
                EventId = r.EventId,
                UserId = r.UserId,
                UserName  = r.User?.Name  ?? string.Empty,
                UserEmail = r.User?.Email ?? string.Empty,
                RegisteredAt = r.RegisteredAt,
                Status = r.Status
            };
        }
    }
}