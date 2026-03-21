using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Event;

public interface IEventService
{
    Task<EventResponseDTO> CreateEventAsync(CreateEventRequestDTO dto);

    Task<IEnumerable<EventResponseDTO>> GetAllAsync();

    Task<EventResponseDTO> GetByIdAsync(int id);

    Task<EventResponseDTO> DeleteAsync(int id);

    Task<EventResponseDTO> ApproveAsync(int eventId, int adminId);

    Task<EventResponseDTO> RejectAsync(int eventId, int adminId);

    //  FIXED SIGNATURE
    Task<EventResponseDTO> CancelEventAsync(int eventId, int userId, string role);

    Task<IEnumerable<EventResponseDTO>> SearchAsync(string keyword);

    Task<IEnumerable<EventResponseDTO>> GetByDateRangeAsync(DateTime start, DateTime end);

    Task<PagedResultDTO<EventResponseDTO>> GetPagedAsync(int pageNumber, int pageSize);

    Task<IEnumerable<EventResponseDTO>> GetMyEventsAsync(int userId);

    Task<IEnumerable<EventResponseDTO>> GetRegisteredEventsAsync(int userId);
    Task<RefundSummaryDTO> GetRefundSummaryAsync(int eventId);
    Task<IEnumerable<EventResponseDTO>> GetPendingEventsAsync();

    Task<IEnumerable<EventResponseDTO>> GetRejectedEventsAsync();

    Task<IEnumerable<EventResponseDTO>> GetApprovedEventsAsync();
}