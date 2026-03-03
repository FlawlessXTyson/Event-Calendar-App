using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.User;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using BCrypt.Net;

namespace EventCalenderApi.Services
{
    public class UserService : IUserService
    {
        private readonly IRepository<int, User> _userRepository;

        public UserService(IRepository<int, User> userRepository)
        {
            _userRepository = userRepository;
        }

        // =========================================
        // CREATE USER (Admin Manual Creation)
        // =========================================
        public async Task<CreateUserResponseDTO> CreateUserAsync(CreateUserRequestDTO request)
        {
            var users = await _userRepository.GetAllAsync();

            if (users.Any(u => u.Email == request.Email))
                throw new Exception("User with this email already exists.");

            // 🔥 Use BCrypt (same as AuthenticationService)
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = passwordHash,
                Role = request.Role,
                Status = AccountStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepository.AddAsync(user);

            return MapToResponseDTO(createdUser);
        }

        // =========================================
        // GET USER BY ID
        // =========================================
        public async Task<CreateUserResponseDTO?> GetUserByIdAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
                return null;

            return MapToResponseDTO(user);
        }

        // =========================================
        // GET ALL USERS (Admin)
        // =========================================
        public async Task<IEnumerable<CreateUserResponseDTO>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();

            return users.Select(u => MapToResponseDTO(u));
        }

        // =========================================
        // UPDATE USER
        // =========================================
        public async Task<CreateUserResponseDTO?> UpdateUserAsync(int userId, UpdateUserRequestDTO request)
        {
            var existingUser = await _userRepository.GetByIdAsync(userId);

            if (existingUser == null)
                return null;

            existingUser.Name = request.Name ?? existingUser.Name;
            existingUser.Email = request.Email ?? existingUser.Email;
            existingUser.Role = request.Role ?? existingUser.Role;
            existingUser.Status = request.Status ?? existingUser.Status;

            var updatedUser = await _userRepository.UpdateAsync(userId, existingUser);

            if (updatedUser == null)
                return null;

            return MapToResponseDTO(updatedUser);
        }

        // =========================================
        // DELETE USER
        // =========================================
        public async Task<bool> DeleteUserAsync(int userId)
        {
            var deletedUser = await _userRepository.DeleteAsync(userId);

            return deletedUser != null;
        }

        // =========================================
        // ENTITY TO DTO
        // =========================================
        private CreateUserResponseDTO MapToResponseDTO(User user)
        {
            return new CreateUserResponseDTO
            {
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                Status = user.Status,
                CreatedAt = user.CreatedAt
            };
        }
    }
}