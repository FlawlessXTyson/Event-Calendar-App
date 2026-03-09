using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.EventRegisration;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;

namespace EventCalenderApi.Services
{
    public class EventRegistrationService : IEventRegistrationService
    {
        private readonly IRepository<int, EventRegistration> _registrationRepo;
        private readonly IRepository<int, Event> _eventRepo;

        public EventRegistrationService(
            IRepository<int, EventRegistration> registrationRepo,
            IRepository<int, Event> eventRepo)
        {
            _registrationRepo = registrationRepo;
            _eventRepo = eventRepo;
        }

        // =========================================
        // REGISTER FOR EVENT
        // =========================================
        public async Task<EventRegistrationResponseDTO> RegisterAsync(EventRegisterationRequestDTO dto)
        {
            var ev = await _eventRepo.GetByIdAsync(dto.EventId);

            if (ev == null)
                throw new NotFoundException("Event not found.");

            // Event not approved
            if (ev.ApprovalStatus != ApprovalStatus.APPROVED)
                throw new BadRequestException("Event is not approved.");

            // Event cancelled
            if (ev.Status == EventStatus.CANCELLED)
                throw new BadRequestException("This event has been cancelled.");

            // Event completed
            if (ev.Status == EventStatus.COMPLETED)
                throw new BadRequestException("This event has already been completed.");

            // Duplicate registration check
            var allRegistrations = await _registrationRepo.GetAllAsync();

            var existingRegistration = allRegistrations
                .FirstOrDefault(r =>
                    r.EventId == dto.EventId &&
                    r.UserId == dto.UserId &&
                    r.Status != RegistrationStatus.CANCELLED);

            if (existingRegistration != null)
                throw new BadRequestException("You are already registered for this event.");

            // Create registration
            var registration = new EventRegistration
            {
                EventId = dto.EventId,
                UserId = dto.UserId,
                RegisteredAt = DateTime.UtcNow,
                Status = RegistrationStatus.REGISTERED
            };

            var created = await _registrationRepo.AddAsync(registration);

            return MapToDTO(created);
        }

        // =========================================
        // CANCEL REGISTRATION
        // =========================================
        public async Task<EventRegistrationResponseDTO?> CancelAsync(
            int registrationId,
            int userId,
            string role)
        {
            var registration = await _registrationRepo.GetByIdAsync(registrationId);

            if (registration == null)
                throw new NotFoundException("Registration not found.");

            // USER can cancel only own registration
            if (role == "USER" && registration.UserId != userId)
                throw new UnauthorizedException("You can cancel only your own registration.");

            registration.Status = RegistrationStatus.CANCELLED;

            var updated = await _registrationRepo.UpdateAsync(registrationId, registration);

            return MapToDTO(updated!);
        }

        // =========================================
        // GET REGISTRATIONS BY EVENT
        // =========================================
        public async Task<IEnumerable<EventRegistrationResponseDTO>> GetByEventAsync(int eventId)
        {
            var all = await _registrationRepo.GetAllAsync();

            return all
                .Where(r => r.EventId == eventId)
                .Select(MapToDTO);
        }

        // =========================================
        // MAPPING
        // =========================================
        private EventRegistrationResponseDTO MapToDTO(EventRegistration registration)
        {
            return new EventRegistrationResponseDTO
            {
                RegistrationId = registration.RegistrationId,
                EventId = registration.EventId,
                UserId = registration.UserId,
                RegisteredAt = registration.RegisteredAt,
                Status = registration.Status
            };
        }
    }
}