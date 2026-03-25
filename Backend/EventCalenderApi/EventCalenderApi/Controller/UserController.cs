using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.User;

namespace EventCalenderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        //create user (admin only)
        [Authorize(Roles = "ADMIN")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateUserRequestDTO request)
        {
            return Ok(await _userService.CreateUserAsync(request));
        }

        //get my profile
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            return Ok(await _userService.GetUserByIdAsync(userId));
        }

        //get all users (admin only)
        [Authorize(Roles = "ADMIN")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _userService.GetAllUsersAsync());
        }

        //update my profile
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile(UpdateUserRequestDTO request)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            return Ok(await _userService.UpdateUserAsync(userId, request));
        }

        //delete user (admin only)
        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _userService.DeleteUserAsync(id);

            return NoContent();
        }

        // disable user (admin only) — soft delete, sets status to BLOCKED
        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}/disable")]
        public async Task<IActionResult> Disable(int id)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _userService.DisableUserAsync(id, adminId));
        }

        // enable user (admin only) — reverses disable, sets status to ACTIVE
        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}/enable")]
        public async Task<IActionResult> Enable(int id)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _userService.EnableUserAsync(id, adminId));
        }
    }
}