using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EventCalenderApi.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IRepository<int, User> _userRepo;
        private readonly IConfiguration _configuration;
        private readonly IAuditLogRepository _auditRepo;

        public AuthenticationService(
            IRepository<int, User> userRepo,
            IConfiguration configuration,
            IAuditLogRepository auditRepo)
        {
            _userRepo = userRepo;
            _configuration = configuration;
            _auditRepo = auditRepo;
        }

        /// <summary>
        /// Registers a new user account using the specified registration details and returns a login response
        /// containing authentication information.
        /// </summary>
        /// <remarks>The method normalizes the email address by trimming whitespace and converting it to
        /// lowercase before checking for duplicates. Upon successful registration, an audit log entry is created for
        /// the registration action.</remarks>
        /// <param name="request">The registration details for the new user, including email, password, and user name. The email and password
        /// fields are required and cannot be null or whitespace.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a login response with
        /// authentication information for the newly registered user.</returns>
        /// <exception cref="BadRequestException">Thrown if the email or password is missing, or if the email is already registered.</exception>
        // REGISTER 
        public async Task<LoginResponseDTO> RegisterAsync(RegisterRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
                throw new BadRequestException("Email and password are required");

            var email = request.Email.Trim().ToLower();

            var existingUser = await _userRepo.GetQueryable()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (existingUser != null)
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

            var created = await _userRepo.AddAsync(user);

            //  AUDIT LOG
            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = created.UserId,
                Role = "USER",
                Action = "REGISTER",
                Entity = "User",
                EntityId = created.UserId
            });

            return GenerateTokenResponse(created);
        }


        /// <summary>
        /// Authenticates a user based on the provided login credentials and returns a token response if successful.
        /// </summary>
        /// <remarks>An audit log entry is created for each successful login. The method does not reveal
        /// whether the email or password was incorrect to prevent information disclosure.</remarks>
        /// <param name="request">An object containing the user's email and password used for authentication. Both fields are required and
        /// must not be null or whitespace.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a token response with
        /// authentication details if the login is successful.</returns>
        /// <exception cref="BadRequestException">Thrown if the email or password in the request is null, empty, or consists only of whitespace.</exception>
        /// <exception cref="UnauthorizedException">Thrown if the credentials are invalid or the user's account is not active.</exception>
        //  LOGIN 
        public async Task<LoginResponseDTO> LoginAsync(LoginRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
                throw new BadRequestException("Email and password are required");

            var email = request.Email.Trim().ToLower();

            var user = await _userRepo.GetQueryable()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null ||
                !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedException("Invalid email or password");

            if (user.Status != AccountStatus.ACTIVE)
                throw new UnauthorizedException("Your account has been disabled. Please contact support.");

            //  AUDIT LOG
            await _auditRepo.AddAsync(new AuditLog
            {
                UserId = user.UserId,
                Role = user.Role.ToString(),
                Action = "LOGIN",
                Entity = "User",
                EntityId = user.UserId
            });

            return GenerateTokenResponse(user);
        }


        /// <summary>
        /// Generates a login response containing a JWT access token for the specified user.
        /// </summary>
        /// <remarks>The generated token includes claims for the user's identifier, name, email, and role.
        /// The token's expiration and other parameters are determined by the application's JWT configuration.</remarks>
        /// <param name="user">The user for whom the JWT access token is generated. Must not be null.</param>
        /// <returns>A LoginResponseDTO containing the generated JWT access token for the user.</returns>
        /// <exception cref="BadRequestException">Thrown if the required JWT configuration is missing from the application settings.</exception>
        //  TOKEN 
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