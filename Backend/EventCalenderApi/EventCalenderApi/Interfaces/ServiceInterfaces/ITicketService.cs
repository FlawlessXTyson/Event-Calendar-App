using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Ticket;

namespace EventCalenderApi.Interfaces.ServiceInterfaces
{
    public interface ITicketService
    {
        Task<TicketResponseDTO> GenerateTicketAsync(int userId, int eventId, int? paymentId);
        Task<TicketResponseDTO?> GetTicketAsync(int userId, int eventId);
        Task<IEnumerable<TicketResponseDTO>> GetMyTicketsAsync(int userId);
    }
}
