using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Todo;

namespace EventCalenderApi.Interfaces.ServiceInterfaces
{
    public interface ITodoService
    {
        Task<CreateTodoResponseDTO> CreateAsync(CreateTodoRequestDTO dto);

        Task<IEnumerable<CreateTodoResponseDTO>> GetByUserAsync(int userId);

        Task MarkCompletedAsync(int todoId, int userId);
    }
}