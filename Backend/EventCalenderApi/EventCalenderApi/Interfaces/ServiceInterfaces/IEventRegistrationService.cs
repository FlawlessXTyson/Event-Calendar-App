using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.EventRegisration;

namespace EventCalenderApi.Interfaces.ServiceInterfaces
{
    public interface IEventRegistrationService
    {
        Task<EventRegistrationResponseDTO> RegisterAsync(EventRegisterationRequestDTO dto, int userId);

        Task<EventRegistrationResponseDTO> CancelAsync(int registrationId, int userId, string role);

        Task<IEnumerable<EventRegistrationResponseDTO>> GetByEventAsync(int eventId);

        Task<PagedResultDTO<EventRegistrationResponseDTO>> GetByEventPagedAsync(int eventId, int pageNumber, int pageSize, DateTime? filterDate);

        Task<IEnumerable<EventRegistrationResponseDTO>> GetMyRegistrationsAsync(int userId);
    }
}