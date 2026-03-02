public class LoginResponseDTO
{
    public string Token { get; set; } = null!;
    public DateTime Expiration { get; set; }

    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;   
}