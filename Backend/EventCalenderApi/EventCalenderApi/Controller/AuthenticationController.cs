using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventCalenderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;

        public AuthenticationController(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        /// <summary>
        /// Registers a new user account using the provided registration details.
        /// </summary>
        /// <param name="request">The registration information for the new user. Must contain valid and complete data as required by the
        /// registration process.</param>
        /// <returns>An IActionResult containing the result of the registration operation. Returns a success response with
        /// registration details if the operation is successful.</returns>
        /// <exception cref="BadRequestException">Thrown when the input data is invalid or does not meet the required model validation criteria.</exception>
        // register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDTO request)
        {
            if (!ModelState.IsValid)
                throw new BadRequestException("Invalid input data");

            var result = await _authenticationService.RegisterAsync(request);
            return Ok(result);
        }


        /// <summary>
        /// Authenticates a user with the provided login credentials.
        /// </summary>
        /// <param name="request">The login request data containing the user's email and password. Must not be null and must satisfy all
        /// validation requirements.</param>
        /// <returns>An IActionResult containing the authentication result if the login is successful.</returns>
        /// <exception cref="BadRequestException">Thrown when the login request data is invalid or does not meet the required format.</exception>
        // login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDTO request)
        {
            if (!ModelState.IsValid)
                throw new BadRequestException("Invalid email or password format");

            var result = await _authenticationService.LoginAsync(request);
            return Ok(result);
        }
    }
}