using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.User;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EventCalenderApi.Services
{
    public class UserService : IUserService
    {
        private readonly IRepository<int, User> _userRepository;

        public UserService(IRepository<int, User> userRepository)
        {
            _userRepository = userRepository;
        }

        //create user manually
        public async Task<CreateUserResponseDTO> CreateUserAsync(CreateUserRequestDTO request)
        {
            var exists = await _userRepository
                .GetQueryable()
                .AnyAsync(u => u.Email == request.Email);

            if (exists)
                throw new BadRequestException("User with this email already exists.");

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


        //get user by the id
        public async Task<CreateUserResponseDTO> GetUserByIdAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
                throw new NotFoundException("User not found.");

            return MapToResponseDTO(user);
        }


        // get all users
        public async Task<IEnumerable<CreateUserResponseDTO>> GetAllUsersAsync()
        {
            var users = await _userRepository
                .GetQueryable()
                .ToListAsync();

            return users.Select(u => MapToResponseDTO(u));
        }


        //update user
        public async Task<CreateUserResponseDTO> UpdateUserAsync(int userId, UpdateUserRequestDTO request)
        {
            var existingUser = await _userRepository.GetByIdAsync(userId);

            if (existingUser == null)
                throw new NotFoundException("User not found.");

            existingUser.Name = request.Name ?? existingUser.Name;
            existingUser.Email = request.Email ?? existingUser.Email;
            existingUser.Role = request.Role ?? existingUser.Role;
            existingUser.Status = request.Status ?? existingUser.Status;

            var updatedUser = await _userRepository.UpdateAsync(userId, existingUser);

            return MapToResponseDTO(updatedUser!);
        }


        //delete user
        public async Task<bool> DeleteUserAsync(int userId)
        {
            var deletedUser = await _userRepository.DeleteAsync(userId);

            if (deletedUser == null)
                throw new NotFoundException("User not found.");

            return true;
        }


        //entity to DTO mapping
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