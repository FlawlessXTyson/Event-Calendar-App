using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.EventRegisration;
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

        // ============================
        // REGISTER
        // ============================
        public async Task<EventRegistrationResponseDTO> RegisterAsync(EventRegisterationRequestDTO dto)
        {
            var ev = await _eventRepo.GetByIdAsync(dto.EventId);

            if (ev == null)
                throw new Exception("Event not found.");

            if (ev.ApprovalStatus != ApprovalStatus.APPROVED)
                throw new Exception("Event is not approved.");

            var registration = new EventRegistration
            {
                EventId = dto.EventId,
                UserId = dto.UserId,
                RegisteredAt = DateTime.Now,
                Status = RegistrationStatus.REGISTERED
            };

            var created = await _registrationRepo.AddAsync(registration);

            return MapToDTO(created);
        }

        // ============================
        // CANCEL
        // ============================
        public async Task<EventRegistrationResponseDTO?> CancelAsync(
            int registrationId,
            int userId,
            string role)
        {
            var registration = await _registrationRepo.GetByIdAsync(registrationId);

            if (registration == null)
                return null;

            // USER can cancel only own registration
            if (role == "USER" && registration.UserId != userId)
                return null;

            registration.Status = RegistrationStatus.CANCELLED;

            var updated = await _registrationRepo.UpdateAsync(registrationId, registration);

            return MapToDTO(updated!);
        }

        // ============================
        // GET BY EVENT
        // ============================
        public async Task<IEnumerable<EventRegistrationResponseDTO>> GetByEventAsync(int eventId)
        {
            var all = await _registrationRepo.GetAllAsync();

            return all
                .Where(r => r.EventId == eventId)
                .Select(MapToDTO);
        }

        // ============================
        // MAPPING
        // ============================
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