using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Remainder;

namespace EventCalenderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReminderController : ControllerBase
    {
        private readonly IReminderService _service;

        public ReminderController(IReminderService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateReminderRequestDTO dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            return Ok(await _service.CreateAsync(dto, userId));
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyReminders()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            return Ok(await _service.GetByUserAsync(userId));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            await _service.DeleteAsync(id, userId);

            return NoContent();
        }
    }
}