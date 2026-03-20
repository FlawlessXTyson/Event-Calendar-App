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

        //constructor injection
        public UserService(IRepository<int, User> userRepository)
        {
            _userRepository = userRepository;
        }

        //create user manually (admin)
        public async Task<CreateUserResponseDTO> CreateUserAsync(CreateUserRequestDTO request)
        {
            //validate input
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
                throw new BadRequestException("Email and password are required");

            var email = request.Email.Trim().ToLower();

            //check duplicate email
            var exists = await _userRepository
                .GetQueryable()
                .AnyAsync(u => u.Email == email);

            if (exists)
                throw new BadRequestException("User with this email already exists");

            //hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            //create user entity
            var user = new User
            {
                Name = request.Name,
                Email = email,
                PasswordHash = passwordHash,
                Role = request.Role,
                Status = AccountStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow
            };

            var createdUser = await _userRepository.AddAsync(user);

            return MapToDTO(createdUser);
        }

        //get user by id
        public async Task<CreateUserResponseDTO> GetUserByIdAsync(int userId)
        {
            //fetch user
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new NotFoundException("User not found");

            return MapToDTO(user);
        }

        //get all users
        public async Task<IEnumerable<CreateUserResponseDTO>> GetAllUsersAsync()
        {
            var users = await _userRepository
                .GetQueryable()
                .ToListAsync();

            //map list to DTO
            return users.Select(MapToDTO);
        }

        //update user profile
        public async Task<CreateUserResponseDTO> UpdateUserAsync(int userId, UpdateUserRequestDTO request)
        {
            //fetch user
            var existingUser = await _userRepository.GetByIdAsync(userId)
                ?? throw new NotFoundException("User not found");

            //update email with duplicate check
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var email = request.Email.Trim().ToLower();

                var exists = await _userRepository
                    .GetQueryable()
                    .AnyAsync(u => u.Email == email && u.UserId != userId);

                if (exists)
                    throw new BadRequestException("Email already in use");

                existingUser.Email = email;
            }

            //update other fields
            existingUser.Name = request.Name ?? existingUser.Name;
            existingUser.Role = request.Role ?? existingUser.Role;
            existingUser.Status = request.Status ?? existingUser.Status;

            var updatedUser = await _userRepository.UpdateAsync(userId, existingUser);

            return MapToDTO(updatedUser!);
        }

        //delete user
        public async Task DeleteUserAsync(int userId)
        {
            //delete user from database
            var deletedUser = await _userRepository.DeleteAsync(userId)
                ?? throw new NotFoundException("User not found");
        }

        //helper method for mapping entity → DTO
        private static CreateUserResponseDTO MapToDTO(User user)
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