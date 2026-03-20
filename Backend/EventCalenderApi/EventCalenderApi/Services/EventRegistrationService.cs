using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.EventRegisration;
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

        public EventRegistrationService(
            IRepository<int, EventRegistration> registrationRepo,
            IRepository<int, Event> eventRepo,
            IRepository<int, Payment> paymentRepo)
        {
            _registrationRepo = registrationRepo;
            _eventRepo = eventRepo;
            _paymentRepo = paymentRepo;
        }

        // ✅ REGISTER
        public async Task<EventRegistrationResponseDTO> RegisterAsync(EventRegisterationRequestDTO dto, int userId)
        {
            var ev = await _eventRepo.GetByIdAsync(dto.EventId)
                ?? throw new NotFoundException("Event not found");

            if (ev.ApprovalStatus != ApprovalStatus.APPROVED)
                throw new BadRequestException("Event is not approved");

            if (ev.Status != EventStatus.ACTIVE)
                throw new BadRequestException("Event is not active");

            // 🔥 DEADLINE VALIDATION
            if (ev.RegistrationDeadline != null && ev.RegistrationDeadline < DateTime.UtcNow)
                throw new BadRequestException("Registration deadline has passed");

            var exists = await _registrationRepo
                .GetQueryable()
                .AnyAsync(r =>
                    r.EventId == dto.EventId &&
                    r.UserId == userId &&
                    r.Status == RegistrationStatus.REGISTERED);

            if (exists)
                throw new BadRequestException("You already registered for this event");

            var registration = new EventRegistration
            {
                EventId = dto.EventId,
                UserId = userId,
                RegisteredAt = DateTime.UtcNow,
                Status = RegistrationStatus.REGISTERED
            };

            var created = await _registrationRepo.AddAsync(registration);

            return MapToDTO(created);
        }

        // ✅ CANCEL + PROPER REFUND
        public async Task<EventRegistrationResponseDTO> CancelAsync(int registrationId, int userId, string role)
        {
            var registration = await _registrationRepo.GetByIdAsync(registrationId)
                ?? throw new NotFoundException("Registration not found");

            if (registration.Status == RegistrationStatus.CANCELLED)
                throw new BadRequestException("Already cancelled");

            if (role == "USER" && registration.UserId != userId)
                throw new UnauthorizedException("Not allowed");

            // 🔥 DIRECT DB QUERY (BEST PRACTICE)
            var payment = await _paymentRepo
                .GetQueryable()
                .FirstOrDefaultAsync(p =>
                    p.UserId == registration.UserId &&
                    p.EventId == registration.EventId &&
                    p.Status == PaymentStatus.SUCCESS);

            // 🔥 FULL REFUND
            if (payment != null)
            {
                payment.Status = PaymentStatus.REFUNDED;
                await _paymentRepo.UpdateAsync(payment.PaymentId, payment);
            }

            registration.Status = RegistrationStatus.CANCELLED;

            var updated = await _registrationRepo.UpdateAsync(registrationId, registration);

            return MapToDTO(updated!);
        }

        public async Task<IEnumerable<EventRegistrationResponseDTO>> GetByEventAsync(int eventId)
        {
            var data = await _registrationRepo
                .GetQueryable()
                .Where(r => r.EventId == eventId)
                .ToListAsync();

            return data.Select(MapToDTO);
        }

        public async Task<IEnumerable<EventRegistrationResponseDTO>> GetMyRegistrationsAsync(int userId)
        {
            var data = await _registrationRepo
                .GetQueryable()
                .Where(r => r.UserId == userId && r.Status == RegistrationStatus.REGISTERED)
                .ToListAsync();

            return data.Select(MapToDTO);
        }

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