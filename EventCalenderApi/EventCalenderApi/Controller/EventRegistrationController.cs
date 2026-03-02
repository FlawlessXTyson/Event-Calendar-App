using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.EventRegisration;

namespace EventCalenderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EventRegistrationController : ControllerBase
    {
        private readonly IEventRegistrationService _service;

        public EventRegistrationController(IEventRegistrationService service)
        {
            _service = service;
        }

        // ============================
        // REGISTER FOR EVENT (USER)
        // ============================
        [Authorize(Roles = "USER")]
        [HttpPost]
        public async Task<IActionResult> Register(EventRegisterationRequestDTO dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            dto.UserId = userId; // Never trust client input

            var result = await _service.RegisterAsync(dto);

            return Ok(result);
        }

        // ============================
        // CANCEL REGISTRATION
        // USER → Own registration
        // ADMIN → Any registration
        // ============================
        [Authorize(Roles = "USER,ADMIN")]
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var result = await _service.CancelAsync(id, userId, role!);

            if (result == null)
                return Forbid("Not allowed to cancel this registration.");

            return Ok(result);
        }

        // ============================
        // VIEW REGISTRATIONS FOR EVENT
        // ORGANIZER or ADMIN
        // ============================
        [Authorize(Roles = "ADMIN,ORGANIZER")]
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetByEvent(int eventId)
        {
            return Ok(await _service.GetByEventAsync(eventId));
        }
    }
}