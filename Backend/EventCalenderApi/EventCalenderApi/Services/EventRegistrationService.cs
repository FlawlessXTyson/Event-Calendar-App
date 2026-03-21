using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
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
        private readonly IAuditLogRepository _auditRepo; // ✅ NEW

        public EventRegistrationService(
            IRepository<int, EventRegistration> registrationRepo,
            IRepository<int, Event> eventRepo,
            IRepository<int, Payment> paymentRepo,
            IAuditLogRepository auditRepo) // ✅ NEW
        {
            _registrationRepo = registrationRepo;
            _eventRepo = eventRepo;
            _paymentRepo = paymentRepo;
            _auditRepo = auditRepo; // ✅ NEW
        }

        // ================= REGISTER =================
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

            if (ev.EventDate < DateTime.UtcNow.Date)
                throw new BadRequestException("Event already started");

            if (!ev.IsPaidEvent && ev.SeatsLimit != null)
            {
                var totalRegistered = await _registrationRepo
                    .GetQueryable()
                    .CountAsync(r =>
                        r.EventId == dto.EventId &&
                        r.Status == RegistrationStatus.REGISTERED);

                if (totalRegistered >= ev.SeatsLimit)
                    throw new BadRequestException("No seats available");
            }

            var exists = await _registrationRepo
                .GetQueryable()
                .AnyAsync(r =>
                    r.EventId == dto.EventId &&
                    r.UserId == userId &&
                    r.Status == RegistrationStatus.REGISTERED);

            if (exists)
                throw new BadRequestException("You have already registered for this event");

            var registration = new EventRegistration
            {
                EventId = dto.EventId,
                UserId = userId,
                RegisteredAt = DateTime.UtcNow,
                Status = RegistrationStatus.REGISTERED
            };

            var created = await _registrationRepo.AddAsync(registration);

            // ✅ AUDIT LOG
            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = userId,
                Role = "USER",
                Action = "REGISTER_EVENT",
                Entity = "EventRegistration",
                EntityId = created.RegistrationId
            });

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

            if (ev.EventDate <= DateTime.UtcNow)
                throw new BadRequestException("Cannot cancel after event has started");

            var payment = await _paymentRepo
                .GetQueryable()
                .FirstOrDefaultAsync(p =>
                    p.UserId == registration.UserId &&
                    p.EventId == registration.EventId &&
                    p.Status == PaymentStatus.SUCCESS);

            if (payment != null)
            {
                payment.Status = PaymentStatus.REFUNDED;
                payment.RefundedAmount = payment.AmountPaid;
                payment.RefundedAt = DateTime.UtcNow;

                payment.CommissionAmount = 0;
                payment.OrganizerAmount = 0;

                await _paymentRepo.UpdateAsync(payment.PaymentId, payment);
            }

            registration.Status = RegistrationStatus.CANCELLED;

            var updated = await _registrationRepo.UpdateAsync(registrationId, registration);

            // ✅ AUDIT LOG
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
                .Where(r => r.EventId == eventId)
                .ToListAsync();

            return data.Select(MapToDTO);
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
                RegisteredAt = r.RegisteredAt,
                Status = r.Status
            };
        }
    }
}