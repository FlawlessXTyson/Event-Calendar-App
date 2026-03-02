namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.User
{
    public class CreateUserResponseDTO
    {
        public int UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public UserRole Role { get; set; }

        public AccountStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}