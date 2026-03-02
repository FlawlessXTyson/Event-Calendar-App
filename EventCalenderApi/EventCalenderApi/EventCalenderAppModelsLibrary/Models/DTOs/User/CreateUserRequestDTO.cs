using System.ComponentModel.DataAnnotations;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.User
{
    public class CreateUserRequestDTO
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.USER;
    }
}