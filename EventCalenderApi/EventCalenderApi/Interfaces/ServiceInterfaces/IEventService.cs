using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Event;

namespace EventCalenderApi.Interfaces.ServiceInterfaces
{
    public interface IEventService
    {
        Task<EventResponseDTO> CreateEventAsync(CreateEventRequestDTO dto);

        Task<IEnumerable<EventResponseDTO>> GetAllAsync();

        Task<EventResponseDTO?> GetByIdAsync(int id);

        Task<EventResponseDTO?> DeleteAsync(int id);

        Task<EventResponseDTO?> ApproveAsync(int eventId, int adminId);

        Task<EventResponseDTO?> RejectAsync(int eventId, int adminId);

        Task<EventResponseDTO?> CancelEventAsync(int eventId);

        Task<IEnumerable<EventResponseDTO>> SearchAsync(string keyword);

        Task<IEnumerable<EventResponseDTO>> GetByDateRangeAsync(DateTime start, DateTime end);

        Task<PagedResultDTO<EventResponseDTO>> GetPagedAsync(int pageNumber, int pageSize);
    }
}