using System.ComponentModel.DataAnnotations;

public class LoginRequestDTO
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } =null !;
    [Required]
    public string Password { get; set; } = null !;
}