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

        public EventRegistrationService(
            IRepository<int, EventRegistration> registrationRepo,
            IRepository<int, Event> eventRepo)
        {
            _registrationRepo = registrationRepo;
            _eventRepo = eventRepo;
        }

        public async Task<EventRegistrationResponseDTO> RegisterAsync(EventRegisterationRequestDTO dto)
        {
            var ev = await _eventRepo.GetByIdAsync(dto.EventId);

            if (ev == null)
                throw new NotFoundException("Event not found.");

            if (ev.ApprovalStatus != ApprovalStatus.APPROVED)
                throw new BadRequestException("Event is not approved.");

            if (ev.Status != EventStatus.ACTIVE)
                throw new BadRequestException("Event is not active.");

            var exists = await _registrationRepo
                .GetQueryable()
                .AnyAsync(r =>
                    r.EventId == dto.EventId &&
                    r.UserId == dto.UserId &&
                    r.Status != RegistrationStatus.CANCELLED);

            if (exists)
                throw new BadRequestException("You are already registered.");

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

        public async Task<EventRegistrationResponseDTO?> CancelAsync(int registrationId, int userId, string role)
        {
            var registration = await _registrationRepo.GetByIdAsync(registrationId);

            if (registration == null)
                throw new NotFoundException("Registration not found.");

            if (role == "USER" && registration.UserId != userId)
                throw new UnauthorizedException("You can cancel only your own registration.");

            registration.Status = RegistrationStatus.CANCELLED;

            var updated = await _registrationRepo.UpdateAsync(registrationId, registration);

            return MapToDTO(updated!);
        }

        public async Task<IEnumerable<EventRegistrationResponseDTO>> GetByEventAsync(int eventId)
        {
            var registrations = await _registrationRepo
                .GetQueryable()
                .Where(r => r.EventId == eventId)
                .ToListAsync();

            return registrations.Select(MapToDTO);
        }

        private EventRegistrationResponseDTO MapToDTO(EventRegistration r)
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