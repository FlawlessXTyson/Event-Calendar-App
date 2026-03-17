using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Remainder;

namespace EventCalenderApi.Interfaces.ServiceInterfaces
{
    public interface IReminderService
    {
        Task<CreateReminderResponseDTO> CreateAsync(CreateReminderRequestDTO dto);

        Task<IEnumerable<CreateReminderResponseDTO>> GetByUserAsync(int userId);

        Task DeleteAsync(int reminderId);
    }
}