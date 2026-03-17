public interface IAuthenticationService
{
    Task<LoginResponseDTO> RegisterAsync(RegisterRequestDTO request);
    Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request);
}