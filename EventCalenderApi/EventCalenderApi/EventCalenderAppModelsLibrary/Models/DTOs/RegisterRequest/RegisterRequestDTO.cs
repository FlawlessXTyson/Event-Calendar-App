using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

public class RegisterRequestDTO
{
    [Required]
    [MinLength(3)]
    [JsonPropertyName("username")]
    public string UserName { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = null!;
}