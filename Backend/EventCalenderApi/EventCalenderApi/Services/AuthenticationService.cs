using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventCalenderApi.EventCalenderAppDataLibrary;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EventCalenderApi.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly EventCalendarDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthenticationService(
            EventCalendarDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // register
        public async Task<LoginResponseDTO> RegisterAsync(RegisterRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
                throw new BadRequestException("Email and password are required");

            var email = request.Email.Trim().ToLower();

            if (await _context.Users.AnyAsync(u => u.Email == email))
                throw new BadRequestException("Email already registered");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Name = request.UserName,
                Email = email,
                PasswordHash = passwordHash,
                Role = UserRole.USER,
                Status = AccountStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return GenerateTokenResponse(user);
        }

        // login
        public async Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
                throw new BadRequestException("Email and password are required");

            var email = request.Email.Trim().ToLower();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null ||
                !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedException("Invalid email or password");

            if (user.Status != AccountStatus.ACTIVE)
                throw new UnauthorizedException("Account is not active");

            return GenerateTokenResponse(user);
        }

        // token generation
        private LoginResponseDTO GenerateTokenResponse(User user)
        {
            var jwtSection = _configuration.GetSection("Jwt");

            var jwtKey = jwtSection["Key"]
                ?? throw new BadRequestException("JWT configuration missing");

            var jwtIssuer = jwtSection["Issuer"];
            var jwtAudience = jwtSection["Audience"];
            var duration = int.Parse(jwtSection["DurationInMinutes"] ?? "60");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var expiry = DateTime.UtcNow.AddMinutes(duration);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiry,
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new LoginResponseDTO
            {
                Token = tokenString
            };
        }
    }
}