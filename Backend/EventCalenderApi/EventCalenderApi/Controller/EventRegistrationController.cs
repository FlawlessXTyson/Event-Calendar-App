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


        /// <summary>
        /// Registers the authenticated user for an event using the provided registration details.
        /// </summary>
        /// <remarks>This action requires the user to be authenticated and in the 'USER' role. The user ID
        /// is determined from the current authentication context and is not supplied in the request body.</remarks>
        /// <param name="dto">An object containing the event registration information. Must not be null.</param>
        /// <returns>An IActionResult containing a success message and the registration result data.</returns>
        // REGISTER (NO USERID FROM BODY)
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


        /// <summary>
        /// Cancels the specified registration and processes a refund if applicable.
        /// </summary>
        /// <param name="id">The unique identifier of the registration to cancel.</param>
        /// <returns>An IActionResult containing a success message and the details of the cancelled registration.</returns>
        /// <exception cref="UnauthorizedException">Thrown if the user's role cannot be determined from the current context.</exception>
        // CANCEL (WITH REFUND)
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


        /// <summary>
        /// Retrieves all items associated with the specified event.
        /// </summary>
        /// <param name="eventId">The unique identifier of the event for which to retrieve items.</param>
        /// <returns>An IActionResult containing the collection of items related to the specified event.</returns>
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