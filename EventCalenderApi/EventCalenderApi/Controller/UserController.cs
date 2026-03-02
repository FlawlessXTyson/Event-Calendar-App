using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.User;

namespace EventCalenderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All endpoints require login
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // =========================================
        // CREATE USER (Admin Only)
        // =========================================
        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateUserRequestDTO request)
        {
            var result = await _userService.CreateUserAsync(request);
            return Ok(result);
        }

        // =========================================
        // GET MY PROFILE
        // =========================================
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _userService.GetUserByIdAsync(userId);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // =========================================
        // GET ALL USERS (Admin Only)
        // =========================================
        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _userService.GetAllUsersAsync();
            return Ok(result);
        }

        // =========================================
        // UPDATE MY PROFILE
        // =========================================
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile(UpdateUserRequestDTO request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _userService.UpdateUserAsync(userId, request);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // =========================================
        // DELETE USER (Admin Only)
        // =========================================
        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _userService.DeleteUserAsync(id);

            if (!success)
                return NotFound();

            return Ok("Deleted successfully");
        }
    }
}