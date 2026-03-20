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

        // register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDTO request)
        {
            if (!ModelState.IsValid)
                throw new BadRequestException("Invalid input data");

            var result = await _authenticationService.RegisterAsync(request);
            return Ok(result);
        }

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