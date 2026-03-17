using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.User;

namespace EventCalenderApi.Interfaces.ServiceInterfaces
{
    public interface IUserService
    {
        Task<CreateUserResponseDTO> CreateUserAsync(CreateUserRequestDTO request);

        Task<CreateUserResponseDTO> GetUserByIdAsync(int userId);

        Task<IEnumerable<CreateUserResponseDTO>> GetAllUsersAsync();

        Task<CreateUserResponseDTO> UpdateUserAsync(int userId, UpdateUserRequestDTO request);

        Task<bool> DeleteUserAsync(int userId);
    }
}