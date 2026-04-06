using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Ticket;

namespace EventCalenderApi.Interfaces.ServiceInterfaces
{
    public interface IEmailService
    {
        Task SendTicketEmailAsync(string toEmail, string toName, TicketResponseDTO ticket);
    }
}
