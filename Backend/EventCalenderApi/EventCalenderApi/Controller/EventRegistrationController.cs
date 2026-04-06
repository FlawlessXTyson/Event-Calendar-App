using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.EventRegisration;
using EventCalenderApi.Exceptions;

namespace EventCalenderApi.Controllers
{

    /// <summary>
    /// Provides API endpoints for managing event registrations, including registering for events, cancelling
    /// registrations, retrieving event-related items, and listing the authenticated user's registrations.  
    /// </summary>
    /// <remarks>All actions require authentication. Role-based authorization is enforced for specific
    /// endpoints: only users with the 'USER' role can register for events, 'USER' or 'ADMIN' roles can cancel
    /// registrations, and 'ADMIN' or 'ORGANIZER' roles can retrieve event items. The controller relies on the current
    /// authentication context to determine the user identity and role for each request.</remarks>
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

        // GET /api/EventRegistration/event/{eventId}/paged?pageNumber=1&pageSize=10&filterDate=2026-03-28
        [Authorize(Roles = "ADMIN,ORGANIZER")]
        [HttpGet("event/{eventId}/paged")]
        public async Task<IActionResult> GetByEventPaged(int eventId, int pageNumber = 1, int pageSize = 10, string? filterDate = null)
        {
            DateTime? date = null;
            if (!string.IsNullOrEmpty(filterDate) && DateTime.TryParse(filterDate, out var parsed))
                date = parsed;
            return Ok(await _service.GetByEventPagedAsync(eventId, pageNumber, pageSize, date));
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyRegistrations()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var data = await _service.GetMyRegistrationsAsync(userId);
            return Ok(data); // always return array, never 404
        }
    }
}