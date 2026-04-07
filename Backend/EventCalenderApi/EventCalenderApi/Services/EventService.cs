using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
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
        private readonly INotificationService _notifSvc;

        public EventService(
            IRepository<int, Event> eventRepo,
            IRepository<int, User> userRepo,
            IRepository<int, EventRegistration> registrationRepo,
            IRepository<int, Payment> paymentRepo,
            IAuditLogRepository auditRepo,
            IWalletService walletSvc,
            INotificationService notifSvc)
        {
            _eventRepo = eventRepo;
            _userRepo = userRepo;
            _registrationRepo = registrationRepo;
            _paymentRepo = paymentRepo;
            _auditRepo = auditRepo;
            _walletSvc = walletSvc;
            _notifSvc = notifSvc;
        }
        /// <summary>
        /// Creates a new event based on the specified event details.
        /// </summary>
        /// <remarks>This method enforces several business rules to ensure event validity, such as
        /// preventing duplicate events by the same user on the same date and time, and validating registration
        /// deadlines. The event is created with default public visibility and pending approval status.</remarks>
        /// <param name="dto">An object containing the details required to create the event, including title, date, time, location, and
        /// other event-specific information. Must not specify a past date or time, and must meet all event creation
        /// constraints.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an EventResponseDTO with the
        /// details of the newly created event.</returns>
        /// <exception cref="BadRequestException">Thrown if the event details are invalid, such as specifying a past date or time, an end time before the
        /// start time, a registration deadline after the event start, a duplicate event, or a missing or invalid ticket
        /// price for paid events.</exception>
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

            return MapToDTO(created, 0); // entity-->DTO
        }
        /// <summary>
        /// Asynchronously retrieves all active and approved events, including information about the number of bookings
        /// for each event.
        /// </summary>
        /// <remarks>Each event in the result includes the count of successful bookings, determined by
        /// payment or registration status depending on whether the event is paid or free.</remarks>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of event response
        /// DTOs for all active and approved events. The collection is empty if no such events exist.</returns>
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
        /// <summary>
        /// Asynchronously retrieves an event by its unique identifier and returns its details.
        /// </summary>
        /// <param name="id">The unique identifier of the event to retrieve. Must be a valid event ID.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an EventResponseDTO with the
        /// event details and the number of booked participants.</returns>
        /// <exception cref="NotFoundException">Thrown if no event with the specified ID exists.</exception>
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
        /// <summary>
        /// Deletes the event with the specified identifier asynchronously.
        /// </summary>
        /// <param name="id">The unique identifier of the event to delete.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an EventResponseDTO representing
        /// the deleted event.</returns>
        /// <exception cref="NotFoundException">Thrown if no event with the specified identifier is found.</exception>
        // ================= DELETE =================
        public async Task<EventResponseDTO> DeleteAsync(int id)
        {
            var deleted = await _eventRepo.DeleteAsync(id)
                ?? throw new NotFoundException("Event not found");

            return MapToDTO(deleted, 0);
        }
        /// <summary>
        /// Cancels an event as an administrator or organizer, processes refunds for all successful payments, and
        /// updates the event and registration statuses accordingly.
        /// </summary>
        /// <remarks>Refunds are processed for all successful payments associated with the event. If the
        /// event is cancelled less than 48 hours before its scheduled start, additional compensation or penalties may
        /// apply depending on the role. All active registrations are marked as cancelled. This method should be called
        /// only by users with appropriate permissions.</remarks>
        /// <param name="eventId">The unique identifier of the event to cancel.</param>
        /// <param name="userId">The unique identifier of the user performing the cancellation. Must be the event organizer if the role is
        /// "ORGANIZER".</param>
        /// <param name="role">The role of the user performing the cancellation. Must be either "ADMIN" or "ORGANIZER".</param>
        /// <returns>An EventResponseDTO representing the updated state of the cancelled event.</returns>
        /// <exception cref="NotFoundException">Thrown if the specified event does not exist.</exception>
        /// <exception cref="BadRequestException">Thrown if the event has already started and cannot be cancelled.</exception>
        /// <exception cref="UnauthorizedException">Thrown if an organizer attempts to cancel an event they did not create.</exception>
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

            // ── NOTIFICATIONS ──────────────────────────────────────────────
            await _SendCancellationNotificationsAsync(ev, role, userId, payments, adminId, isLessThan48h);

            return MapToDTO(ev, 0);
        }

        // ── Notification helper for event cancellation ────────────────────
        private async Task _SendCancellationNotificationsAsync(
            Event ev, string role, int cancelledByUserId,
            List<Payment> payments, int adminId, bool isLessThan48h)
        {
            float totalRefunded = payments.Sum(p => p.AmountPaid);

            if (role == "ADMIN")
            {
                // Notify ORGANIZER
                string orgMsg = isLessThan48h && ev.IsPaidEvent && payments.Count > 0
                    ? $"Your event '{ev.Title}' was cancelled by Admin. All users have been refunded. " +
                      $"Total refunded: ₹{totalRefunded:F2}. You received 50% compensation per ticket."
                    : $"Your event '{ev.Title}' was cancelled by Admin. All users have been refunded. " +
                      $"Total refunded: ₹{totalRefunded:F2}.";

                await _notifSvc.CreateNotificationAsync(
                    ev.CreatedByUserId,
                    "Event Cancelled by Admin",
                    orgMsg,
                    NotificationType.EVENT_UPDATE);

                // Notify each USER who had a payment
                foreach (var p in payments)
                {
                    await _notifSvc.CreateNotificationAsync(
                        p.UserId,
                        "Event Cancelled — Refund Processed",
                        $"Event '{ev.Title}' was cancelled by Admin. You have received a full refund of ₹{p.AmountPaid:F2}.",
                        NotificationType.REFUND);
                }
            }
            else if (role == "ORGANIZER")
            {
                // Lookup organizer name
                var organizer = await _userRepo.GetByIdAsync(cancelledByUserId);
                string orgName = organizer?.Name ?? "Organizer";

                if (isLessThan48h)
                {
                    // Penalty scenario
                    float totalPenalty = payments.Sum(p => p.AmountPaid - (p.CommissionAmount * 0.02f));
                    float totalDeducted = payments.Sum(p => p.AmountPaid);

                    // Notify ORGANIZER
                    await _notifSvc.CreateNotificationAsync(
                        cancelledByUserId,
                        "Event Cancelled — Late Penalty Applied",
                        $"You cancelled the event '{ev.Title}' within 48 hours. " +
                        $"You paid ₹{totalPenalty:F2} as penalty. Total deducted: ₹{totalDeducted:F2}.",
                        NotificationType.WARNING);

                    // Notify ADMIN
                    if (adminId > 0)
                    {
                        float commission = payments.Sum(p => p.CommissionAmount * 0.02f);
                        await _notifSvc.CreateNotificationAsync(
                            adminId,
                            "Late Event Cancellation by Organizer",
                            $"Late cancellation by Organizer '{orgName}' for event '{ev.Title}'. " +
                            $"Commission earned: ₹{commission:F2}.",
                            NotificationType.INFO);
                    }
                }
                else
                {
                    // Normal cancellation
                    // Notify ORGANIZER
                    await _notifSvc.CreateNotificationAsync(
                        cancelledByUserId,
                        "Event Cancelled",
                        $"You cancelled the event '{ev.Title}'. Refunds have been processed successfully.",
                        NotificationType.INFO);

                    // Notify ADMIN
                    if (adminId > 0)
                    {
                        await _notifSvc.CreateNotificationAsync(
                            adminId,
                            "Organizer Cancelled Event",
                            $"Organizer '{orgName}' cancelled event '{ev.Title}'. All users were refunded successfully.",
                            NotificationType.INFO);
                    }
                }

                // Notify each USER who had a payment (both scenarios)
                foreach (var p in payments)
                {
                    await _notifSvc.CreateNotificationAsync(
                        p.UserId,
                        "Event Cancelled — Refund Processed",
                        $"Event '{ev.Title}' was cancelled by Organizer. You have received a full refund of ₹{p.AmountPaid:F2}.",
                        NotificationType.REFUND);
                }
            }
        }

        /// <summary>
        /// Approves the specified event and records the approval action by the given administrator.
        /// </summary>
        /// <remarks>This method updates the event's approval status and logs the approval action for
        /// auditing purposes.</remarks>
        /// <param name="eventId">The unique identifier of the event to approve.</param>
        /// <param name="adminId">The unique identifier of the administrator performing the approval.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an EventResponseDTO with the
        /// updated event information after approval.</returns>
        /// <exception cref="NotFoundException">Thrown if an event with the specified eventId does not exist.</exception>
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

            await _notifSvc.CreateNotificationAsync(
                ev.CreatedByUserId,
                "Event Approved",
                $"Your event '{ev.Title}' has been approved by Admin. It is now live and visible to users.",
                NotificationType.INFO);

            return MapToDTO(updated!, 0);
        }
        /// <summary>
        /// Rejects the specified event and records the rejection by the given administrator.
        /// </summary>
        /// <remarks>This method updates the event's approval status to rejected and logs the action for
        /// auditing purposes.</remarks>
        /// <param name="eventId">The unique identifier of the event to reject.</param>
        /// <param name="adminId">The unique identifier of the administrator performing the rejection.</param>
        /// <returns>An EventResponseDTO representing the updated event after the rejection.</returns>
        /// <exception cref="NotFoundException">Thrown if an event with the specified eventId does not exist.</exception>
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

            await _notifSvc.CreateNotificationAsync(
                ev.CreatedByUserId,
                "Event Rejected",
                $"Your event '{ev.Title}' has been rejected by Admin. Please review and resubmit if needed.",
                NotificationType.WARNING);

            return MapToDTO(updated!, 0);
        }
        /// <summary>
        /// Asynchronously searches for events with titles that contain the specified keyword.
        /// </summary>
        /// <param name="keyword">The keyword to search for within event titles. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of event data
        /// transfer objects matching the search criteria. The collection is empty if no events are found.</returns>
        // ================= SEARCH =================
        public async Task<IEnumerable<EventResponseDTO>> SearchAsync(string keyword)
        {
            var data = await _eventRepo.GetQueryable() 
                .Where(e => e.Title.Contains(keyword)) 
                .ToListAsync(); // exc query ,fetch from db

            return data.Select(e => MapToDTO(e, 0));
        }
        /// <summary>
        /// Asynchronously retrieves a collection of events that occur within the specified date range.
        /// </summary>
        /// <param name="start">The start date of the range. Events occurring on or after this date are included.</param>
        /// <param name="end">The end date of the range. Events occurring on or before this date are included.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of
        /// event data transfer objects for events within the specified date range. The collection is empty if no events
        /// are found.</returns>
        public async Task<IEnumerable<EventResponseDTO>> GetByDateRangeAsync(DateTime start, DateTime end)
        {
            var data = await _eventRepo.GetQueryable()
                .Where(e => e.EventDate >= start && e.EventDate <= end)
                .ToListAsync();

            return data.Select(e => MapToDTO(e, 0));
        }
        /// <summary>
        /// Retrieves a paged list of events based on the specified page number and page size.
        /// </summary>
        /// <param name="pageNumber">The one-based index of the page to retrieve. Must be greater than or equal to 1.</param>
        /// <param name="pageSize">The maximum number of events to include in the page. Must be greater than 0.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a
        /// PagedResultDTO<EventResponseDTO> with the requested page of events, the total number of records, and paging
        /// information.</returns>
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
        /// <summary>
        /// Asynchronously retrieves all events created by the specified user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose created events are to be retrieved.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of event data
        /// transfer objects representing the events created by the specified user. The collection is empty if the user
        /// has not created any events.</returns>
        public async Task<IEnumerable<EventResponseDTO>> GetMyEventsAsync(int userId)
        {
            var data = await _eventRepo.GetQueryable()
                .Where(e => e.CreatedByUserId == userId)
                .ToListAsync();

            return data.Select(e => MapToDTO(e, 0));
        }
        /// <summary>
        /// Retrieves a paged list of events created by the specified user, optionally filtered by event date.
        /// </summary>
        /// <remarks>Results are ordered by event date and start time in descending order. Use this method
        /// to implement user-specific event listings with pagination and optional date filtering.</remarks>
        /// <param name="userId">The unique identifier of the user whose events are to be retrieved.</param>
        /// <param name="pageNumber">The page number of results to retrieve. Must be greater than or equal to 1.</param>
        /// <param name="pageSize">The maximum number of events to include in a single page. Must be greater than 0.</param>
        /// <param name="filterDate">An optional date to filter events. If specified, only events occurring on this date are included.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a paged result of event data for
        /// the specified user and criteria. If no events match the criteria, the data collection will be empty.</returns>
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
        /// <summary>
        /// Asynchronously retrieves all events for which the specified user is currently registered.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose registered events are to be retrieved. Must be a valid user ID.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of event data
        /// transfer objects representing the events the user is registered for. The collection is empty if the user is
        /// not registered for any events.</returns>
        public async Task<IEnumerable<EventResponseDTO>> GetRegisteredEventsAsync(int userId)
        {
            var data = await _registrationRepo.GetQueryable()
                .Include(r => r.Event)
                .Where(r => r.UserId == userId && r.Status == RegistrationStatus.REGISTERED)
                .Select(r => r.Event!)
                .ToListAsync();

            return data.Select(e => MapToDTO(e, 0));
        }
        /// <summary>
        /// Asynchronously retrieves a summary of refund information for a specified event.
        /// </summary>
        /// <param name="eventId">The unique identifier of the event for which to retrieve refund summary data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a RefundSummaryDTO with the
        /// total number of users refunded and the total refund amount for the specified event.</returns>
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
        /// <summary>
        /// Asynchronously retrieves all pending events that have not yet started.
        /// </summary>
        /// <remarks>Events are considered pending if their approval status is set to pending and their
        /// scheduled start time is in the future relative to the current time. The results are ordered by event date in
        /// descending order before filtering for future events.</remarks>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of event data
        /// transfer objects for events with a pending approval status and a start time later than the current time. The
        /// collection is empty if no such events exist.</returns>
        public async Task<IEnumerable<EventResponseDTO>> GetPendingEventsAsync()
        {
            var data = await _eventRepo.GetQueryable()
                .Include(e => e.CreatedBy)
                .Where(e => e.ApprovalStatus == ApprovalStatus.PENDING)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
            
            var now = IstClock.Now;
            return data.Where(e => e.EventDate.Add(e.StartTime ?? TimeSpan.Zero) > now)
                       .Select(e => MapToDTO(e, 0));
        }
        /// <summary>
        /// Asynchronously retrieves a paged list of upcoming events that are pending approval.
        /// </summary>
        /// <remarks>Only events with a pending approval status and a start time in the future are
        /// included. The results are ordered by event date in descending order.</remarks>
        /// <param name="pageNumber">The 1-based index of the page to retrieve. Must be greater than or equal to 1.</param>
        /// <param name="pageSize">The maximum number of events to include in the page. Must be greater than 0.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a paged result with event data
        /// for pending, upcoming events. If no events are found, the data collection will be empty.</returns>
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
        /// <summary>
        /// Asynchronously retrieves a collection of events that have been rejected.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of
        /// event response data transfer objects for all rejected events, ordered by event date in descending order.</returns>
        public async Task<IEnumerable<EventResponseDTO>> GetRejectedEventsAsync()
        {
            var data = await _eventRepo.GetQueryable()
                .Include(e => e.CreatedBy)
                .Where(e => e.ApprovalStatus == ApprovalStatus.REJECTED)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
            return data.Select(e => MapToDTO(e, 0));
        }
        /// <summary>
        /// Retrieves a paged list of events that have been rejected.
        /// </summary>
        /// <remarks>The results are ordered by event date in descending order. Use this method to
        /// efficiently browse large sets of rejected events in manageable pages.</remarks>
        /// <param name="pageNumber">The 1-based index of the page to retrieve. Must be greater than or equal to 1.</param>
        /// <param name="pageSize">The maximum number of events to include in the page. Must be greater than 0.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a paged result with event data
        /// for the specified page of rejected events.</returns>
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
        /// <summary>
        /// Asynchronously retrieves a collection of events that have been approved.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of
        /// event data transfer objects for approved events, ordered by event date in descending order. The collection
        /// is empty if no approved events are found.</returns>
        public async Task<IEnumerable<EventResponseDTO>> GetApprovedEventsAsync()
        {
            var data = await _eventRepo.GetQueryable()
                .Include(e => e.CreatedBy)
                .Where(e => e.ApprovalStatus == ApprovalStatus.APPROVED)
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();
            return data.Select(e => MapToDTO(e, 0));
        }
        /// <summary>
        /// Retrieves a paged list of approved events, ordered by event date in descending order.
        /// </summary>
        /// <remarks>Only events with an approval status of approved and not cancelled are included in the
        /// results. The method supports efficient paging for large event sets.</remarks>
        /// <param name="pageNumber">The 1-based index of the page to retrieve. Must be greater than or equal to 1.</param>
        /// <param name="pageSize">The maximum number of events to include in the page. Must be greater than 0.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a paged result with event data
        /// for the specified page. If no events are found, the data collection will be empty.</returns>
        public async Task<PagedResultDTO<EventResponseDTO>> GetApprovedEventsPagedAsync(int pageNumber, int pageSize)
        {
            var query = _eventRepo.GetQueryable()
                .Include(e => e.CreatedBy)
                .Where(e => e.ApprovalStatus == ApprovalStatus.APPROVED && e.Status != EventStatus.CANCELLED)
                .OrderByDescending(e => e.EventDate);
            var total = await query.CountAsync();
            var data  = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResultDTO<EventResponseDTO> { PageNumber = pageNumber, PageSize = pageSize, TotalRecords = total, Data = data.Select(e => MapToDTO(e, 0)) };
        }
        /// <summary>
        /// Asynchronously retrieves a collection of events that have expired based on their end date or event date.
        /// </summary>
        /// <remarks>An event is considered expired if its end date, or event date if no end date is
        /// specified, is earlier than the current UTC date. The returned collection includes both paid and free events,
        /// with booking counts calculated according to the event type.</remarks>
        /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of
        /// event response DTOs for all expired events, ordered by event date in descending order.</returns>
        // EXPIRED EVENTS 
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
        /// <summary>
        /// Asynchronously retrieves a paged list of expired events, ordered by event date in descending order.
        /// </summary>
        /// <remarks>An event is considered expired if its end date (if present) or event date is earlier
        /// than the current UTC date. The result includes the total number of expired events and the data for the
        /// specified page.</remarks>
        /// <param name="pageNumber">The 1-based index of the page to retrieve. Must be greater than or equal to 1.</param>
        /// <param name="pageSize">The maximum number of events to include in the page. Must be greater than 0.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a paged result with event data
        /// for expired events.</returns>
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
        /// <summary>
        /// Asynchronously retrieves a paged list of cancelled events.
        /// </summary>
        /// <remarks>The results are ordered by event date in descending order. If there are no cancelled
        /// events, the returned data collection will be empty.</remarks>
        /// <param name="pageNumber">The 1-based index of the page to retrieve. Must be greater than or equal to 1.</param>
        /// <param name="pageSize">The maximum number of events to include in the page. Must be greater than 0.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a paged result with event data
        /// for the specified page of cancelled events.</returns>
        //  CANCELLED EVENTS PAGED 
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
        /// <summary>
        /// Retrieves a paged list of all events, optionally filtered by a search term, for administrative purposes.
        /// </summary>
        /// <remarks>The results are ordered by event date in descending order. If the search term is
        /// provided, only events with a title or location containing the term are included.</remarks>
        /// <param name="pageNumber">The number of the page to retrieve. Must be greater than or equal to 1.</param>
        /// <param name="pageSize">The maximum number of events to include in the returned page. Must be greater than 0.</param>
        /// <param name="search">An optional search term used to filter events by title or location. If null or empty, all events are
        /// included.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a paged result with event data
        /// matching the specified criteria.</returns>
        //  ALL EVENTS PAGED (ADMIN) 
        public async Task<PagedResultDTO<EventResponseDTO>> GetAllEventsPagedAsync(int pageNumber, int pageSize, string? search)
        {
            var query = _eventRepo.GetQueryable()
                .Include(e => e.CreatedBy)
                .OrderByDescending(e => e.EventDate)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search)) //If search is NOT empty → apply filter ,null,empty space
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
        /// <summary>
        /// Maps the specified event entity and the number of booked seats to an event response data transfer object
        /// (DTO).
        /// </summary>
        /// <remarks>If the event has no seat limit, the SeatsLeft property in the returned DTO is set to
        /// -1. The OrganizerName is set to an empty string if the event's creator is not specified.</remarks>
        /// <param name="ev">The event entity containing the source data to be mapped.</param>
        /// <param name="bookedCount">The number of seats that have already been booked for the event. Must be zero or greater.</param>
        /// <returns>An EventResponseDTO instance populated with data from the event entity and the calculated number of seats
        /// left.</returns>
        //  MAPPER no dependcy on object pure helper method to convert Event to EventResponseDTO
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
                HasEnded   = IstClock.Now >  (ev.EventEndDate ?? ev.EventDate).Add(ev.EndTime ?? new TimeSpan(23, 59, 59)),
                Status     = ev.Status
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

            //if deadline passed ->reg closed
            if (ev.RegistrationDeadline != null && ev.RegistrationDeadline < nowUtc)
                return false;

            // Event already started -reg closed
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