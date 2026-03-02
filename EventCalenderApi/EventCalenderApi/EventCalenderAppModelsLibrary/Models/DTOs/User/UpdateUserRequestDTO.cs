using System.ComponentModel.DataAnnotations;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.User
{
    public class UpdateUserRequestDTO
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        public UserRole? Role { get; set; }

        public AccountStatus? Status { get; set; }
    }
}