using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.EventRegisration;
using EventCalenderApi.Exceptions;

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

        // ✅ REGISTER (NO USERID FROM BODY)
        [Authorize(Roles = "USER")]
        [HttpPost]
        public async Task<IActionResult> Register(EventRegisterationRequestDTO dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _service.RegisterAsync(dto, userId);

            return Ok(new
            {
                message = "Successfully registered for event",
                data = result
            });
        }

        // ✅ CANCEL (WITH REFUND)
        [Authorize(Roles = "USER,ADMIN")]
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(role))
                throw new UnauthorizedException("User role not found");

            var result = await _service.CancelAsync(id, userId, role);

            return Ok(new
            {
                message = "Registration cancelled successfully",
                data = result
            });
        }

        [Authorize(Roles = "ADMIN,ORGANIZER")]
        [HttpGet("event/{eventId}")]
        public async Task<IActionResult> GetByEvent(int eventId)
        {
            return Ok(await _service.GetByEventAsync(eventId));
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyRegistrations()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var data = await _service.GetMyRegistrationsAsync(userId);

            if (!data.Any())
                throw new NotFoundException("You have not registered for any events");

            return Ok(data);
        }
    }
}