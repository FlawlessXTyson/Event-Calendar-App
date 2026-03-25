using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.User;

namespace EventCalenderApi.Interfaces.ServiceInterfaces
{
    public interface IUserService
    {
        Task<CreateUserResponseDTO> CreateUserAsync(CreateUserRequestDTO request);

        Task<CreateUserResponseDTO> GetUserByIdAsync(int userId);

        Task<IEnumerable<CreateUserResponseDTO>> GetAllUsersAsync();

        Task<CreateUserResponseDTO> UpdateUserAsync(int userId, UpdateUserRequestDTO request);

        Task DeleteUserAsync(int userId);

        Task<CreateUserResponseDTO> DisableUserAsync(int userId, int adminId);

        Task<CreateUserResponseDTO> EnableUserAsync(int userId, int adminId);
    }
}