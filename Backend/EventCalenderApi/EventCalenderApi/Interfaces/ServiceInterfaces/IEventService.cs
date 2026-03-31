using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Event;

public interface IEventService
{
    Task<EventResponseDTO> CreateEventAsync(CreateEventRequestDTO dto);

    Task<IEnumerable<EventResponseDTO>> GetAllAsync();

    Task<EventResponseDTO> GetByIdAsync(int id);

    Task<EventResponseDTO> DeleteAsync(int id);

    Task<EventResponseDTO> ApproveAsync(int eventId, int adminId);

    Task<EventResponseDTO> RejectAsync(int eventId, int adminId);

    
    Task<EventResponseDTO> CancelEventAsync(int eventId, int userId, string role);

    Task<IEnumerable<EventResponseDTO>> SearchAsync(string keyword);

    Task<IEnumerable<EventResponseDTO>> GetByDateRangeAsync(DateTime start, DateTime end);

    Task<PagedResultDTO<EventResponseDTO>> GetPagedAsync(int pageNumber, int pageSize);

    Task<IEnumerable<EventResponseDTO>> GetMyEventsAsync(int userId);

    Task<PagedResultDTO<EventResponseDTO>> GetMyEventsPagedAsync(int userId, int pageNumber, int pageSize, DateTime? filterDate);

    Task<IEnumerable<EventResponseDTO>> GetRegisteredEventsAsync(int userId);
    Task<RefundSummaryDTO> GetRefundSummaryAsync(int eventId);
    Task<IEnumerable<EventResponseDTO>> GetPendingEventsAsync();
    Task<PagedResultDTO<EventResponseDTO>> GetPendingEventsPagedAsync(int pageNumber, int pageSize);

    Task<IEnumerable<EventResponseDTO>> GetRejectedEventsAsync();
    Task<PagedResultDTO<EventResponseDTO>> GetRejectedEventsPagedAsync(int pageNumber, int pageSize);

    Task<IEnumerable<EventResponseDTO>> GetApprovedEventsAsync();
    Task<PagedResultDTO<EventResponseDTO>> GetApprovedEventsPagedAsync(int pageNumber, int pageSize);

    Task<IEnumerable<EventResponseDTO>> GetExpiredEventsAsync();
    Task<PagedResultDTO<EventResponseDTO>> GetExpiredEventsPagedAsync(int pageNumber, int pageSize);

    Task<PagedResultDTO<EventResponseDTO>> GetCancelledEventsPagedAsync(int pageNumber, int pageSize);

    Task<PagedResultDTO<EventResponseDTO>> GetAllEventsPagedAsync(int pageNumber, int pageSize, string? search);
}