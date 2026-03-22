using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Remainder;

namespace EventCalenderApi.Interfaces.ServiceInterfaces
{
    public interface IReminderService
    {
        //create reminder
        Task<CreateReminderResponseDTO> CreateAsync(CreateReminderRequestDTO dto, int userId);

        //get reminder for loggedin users
        Task<IEnumerable<CreateReminderResponseDTO>> GetByUserAsync(int userId);

        //delete reminder
        Task DeleteAsync(int reminderId, int userId);

        Task<IEnumerable<CreateReminderResponseDTO>> GetDueRemindersAsync(int userId);
    }
}