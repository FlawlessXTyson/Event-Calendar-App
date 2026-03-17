using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Remainder;

namespace EventCalenderApi.Interfaces.ServiceInterfaces
{
    public interface IReminderService
    {
        //create reminder (userId comes from token)
        Task<CreateReminderResponseDTO> CreateAsync(CreateReminderRequestDTO dto, int userId);

        //get reminders for logged-in user
        Task<IEnumerable<CreateReminderResponseDTO>> GetByUserAsync(int userId);

        //delete reminder
        Task DeleteAsync(int reminderId);
    }
}