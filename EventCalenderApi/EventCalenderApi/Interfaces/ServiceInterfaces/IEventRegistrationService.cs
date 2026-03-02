using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.EventRegisration;

namespace EventCalenderApi.Interfaces.ServiceInterfaces
{
    public interface IEventRegistrationService
    {
        Task<EventRegistrationResponseDTO> RegisterAsync(EventRegisterationRequestDTO dto);

        Task<EventRegistrationResponseDTO?> CancelAsync(int registrationId, int userId, string role);

        Task<IEnumerable<EventRegistrationResponseDTO>> GetByEventAsync(int eventId);
    }
}