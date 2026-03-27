using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Event;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventCalenderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EventController : ControllerBase
    {
        private readonly IEventService _service;

        public EventController(IEventService service)
        {
            _service = service;
        }


        /// <summary>
        /// Creates a new event using the specified event details.
        /// </summary>
        /// <remarks>This action is restricted to users with the ORGANIZER or ADMIN role. The created
        /// event will have public visibility and will be associated with the authenticated user as the
        /// creator.</remarks>
        /// <param name="dto">The event details to use for creating the new event. Must not be null.</param>
        /// <returns>An IActionResult containing the result of the event creation operation. Returns an HTTP 200 response with
        /// the created event details if successful.</returns>
        /// <exception cref="BadRequestException">Thrown if the request body is null.</exception>
        // CREATE EVENT 
        [Authorize(Roles = "ORGANIZER,ADMIN")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEventRequestDTO dto)
        {
            if (dto == null)
                throw new BadRequestException("Request body cannot be null");

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            dto.CreatedByUserId = userId;
            dto.Visibility = EventVisibility.PUBLIC;

            var result = await _service.CreateEventAsync(dto);

            return Ok(result);
        }


        /// <summary>
        /// Retrieves all approved events.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> containing a collection of approved events. Returns an HTTP 200 response with
        /// the event data if successful.</returns>
        /// <exception cref="NotFoundException">Thrown when no approved events are available.</exception>
        // GET ALL APPROVED 
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result); // return empty array, not 404
        }


        /// <summary>
        /// Retrieves the resource with the specified identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the resource to retrieve.</param>
        /// <returns>An <see cref="IActionResult"/> containing the resource if found; otherwise, a result indicating that the
        /// resource was not found.</returns>
        // ================= GET BY ID =================
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id)
        {
            return Ok(await _service.GetByIdAsync(id));
        }


        /// <summary>
        /// Deletes the resource identified by the specified ID.
        /// </summary>
        /// <remarks>This action requires the caller to have the ADMIN role. The resource is deleted
        /// asynchronously.</remarks>
        /// <param name="id">The unique identifier of the resource to delete.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the delete operation. Returns <see cref="OkResult"/>
        /// if the deletion is successful.</returns>
        // ================= DELETE =================
        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            return Ok(await _service.DeleteAsync(id));
        }


        /// <summary>
        /// Approves the specified item with the given identifier on behalf of the current administrator.
        /// </summary>
        /// <remarks>This action is restricted to users with the ADMIN role. The approval is performed in
        /// the context of the currently authenticated administrator.</remarks>
        /// <param name="id">The unique identifier of the item to approve.</param>
        /// <returns>An IActionResult that represents the result of the approval operation. Returns a 200 OK response with the
        /// approval result if successful.</returns>
        // ================= APPROVE =================
        [Authorize(Roles = "ADMIN")]
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            return Ok(await _service.ApproveAsync(id, adminId));
        }



        
        // ================= REJECT =================
        /// <summary>
        /// Rejects the specified request by its identifier and records the action as performed by the current
        /// administrator.
        /// </summary>
        /// <remarks>This action is restricted to users with the ADMIN role. The administrator performing
        /// the rejection is determined from the current user context.</remarks>
        /// <param name="id">The unique identifier of the request to reject.</param>
        /// <returns>An IActionResult containing the result of the rejection operation.</returns>
        [Authorize(Roles = "ADMIN")]
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(int id)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            return Ok(await _service.RejectAsync(id, adminId));
        }


        /// <summary>
        /// Cancels the event with the specified identifier if the current user is authorized.
        /// </summary>
        /// <remarks>Only users with the ADMIN or ORGANIZER role are permitted to cancel events. The
        /// user's identity and role are determined from the current authentication context.</remarks>
        /// <param name="id">The unique identifier of the event to cancel.</param>
        /// <returns>An IActionResult indicating the result of the cancellation operation. Returns an HTTP 200 response with the
        /// cancellation result if successful.</returns>
        /// <exception cref="UnauthorizedException">Thrown if the user's role cannot be determined or the user is not authorized to cancel the event.</exception>
        // ================= CANCEL EVENT =================
        [Authorize(Roles = "ADMIN,ORGANIZER")]
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(role))
                throw new UnauthorizedException("User role not found");

            return Ok(await _service.CancelEventAsync(id, userId, role));
        }


        /// <summary>
        /// Retrieves a summary of refund information for the specified event or entity.
        /// </summary>
        /// <remarks>This action is restricted to users with the ADMIN or ORGANIZER roles. The returned
        /// summary provides an overview of refund-related details for the specified identifier.</remarks>
        /// <param name="id">The unique identifier of the event or entity for which to obtain the refund summary.</param>
        /// <returns>An IActionResult containing the refund summary data if found; otherwise, a suitable error response.</returns>
        // ================= REFUND SUMMARY =================
        [Authorize(Roles = "ADMIN,ORGANIZER")]
        [HttpGet("{id}/refund-summary")]
        public async Task<IActionResult> GetRefundSummary(int id)
        {
            return Ok(await _service.GetRefundSummaryAsync(id));
        }


        /// <summary>
        /// Searches for events that match the specified keyword.
        /// </summary>
        /// <param name="keyword">The keyword to use when searching for events. Cannot be null.</param>
        /// <returns>An <see cref="IActionResult"/> containing the list of matching events if any are found.</returns>
        /// <exception cref="NotFoundException">Thrown if no events matching the specified keyword are found.</exception>
        // ================= SEARCH =================
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search(string keyword)
        {
            return Ok(await _service.SearchAsync(keyword));
        }


        /// <summary>
        /// Retrieves records that fall within the specified date range.
        /// </summary>
        /// <param name="start">The start date of the range, in yyyy-MM-dd format. Must be less than or equal to <paramref name="end"/>.</param>
        /// <param name="end">The end date of the range, in yyyy-MM-dd format. Must be greater than or equal to <paramref name="start"/>.</param>
        /// <returns>An <see cref="IActionResult"/> containing the records within the specified date range.</returns>
        /// <exception cref="BadRequestException">Thrown if either <paramref name="start"/> or <paramref name="end"/> is not a valid date in yyyy-MM-dd
        /// format, or if <paramref name="start"/> is after <paramref name="end"/>.</exception>
        // ================= DATE RANGE =================
        [HttpGet("range")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByDateRange(string start, string end)
        {
            if (!DateTime.TryParse(start, out var startDate) ||
                !DateTime.TryParse(end, out var endDate))
            {
                throw new BadRequestException("Use format yyyy-MM-dd");
            }

            if (startDate > endDate)
                throw new BadRequestException("Start date must be before end date");

            return Ok(await _service.GetByDateRangeAsync(startDate, endDate));
        }


        /// <summary>
        /// Retrieves a paged list of items based on the specified page number and page size.
        /// </summary>
        /// <param name="pageNumber">The number of the page to retrieve. Must be greater than or equal to 1.</param>
        /// <param name="pageSize">The maximum number of items to include in a single page. Must be greater than 0.</param>
        /// <returns>An IActionResult containing the paged list of items. The result includes the items for the specified page
        /// and may include pagination metadata.</returns>
        // ================= PAGINATION =================
        [HttpGet("paged")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPaged(int pageNumber = 1, int pageSize = 5)
        {
            return Ok(await _service.GetPagedAsync(pageNumber, pageSize));
        }


        /// <summary>
        /// Retrieves a list of events created by the currently authenticated user.
        /// </summary>
        /// <remarks>This action is accessible only to users with the ORGANIZER or ADMIN role. The user is
        /// identified based on the authentication context.</remarks>
        /// <returns>An <see cref="IActionResult"/> containing a collection of the user's events. Returns an HTTP 200 response
        /// with the event data if found.</returns>
        /// <exception cref="NotFoundException">Thrown if the user has not created any events.</exception>
        // ================= MY EVENTS =================
        [Authorize(Roles = "ORGANIZER,ADMIN")]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyEvents()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _service.GetMyEventsAsync(userId));
        }


        /// <summary>
        /// Retrieves the list of events that the currently authenticated user is registered for.
        /// </summary>
        /// <remarks>This endpoint requires the user to be authenticated and in the "USER" role. The
        /// response will contain only the events associated with the current user's account.</remarks>
        /// <returns>An <see cref="IActionResult"/> containing a collection of registered events for the current user.</returns>
        // ================= USER REGISTERED EVENTS =================
        [Authorize(Roles = "USER")]
        [HttpGet("registered")]
        public async Task<IActionResult> GetRegisteredEvents()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            return Ok(await _service.GetRegisteredEventsAsync(userId));
        }


        /// <summary>
        /// Retrieves a list of events that are pending approval.
        /// </summary>
        /// <remarks>This action is restricted to users with the ADMIN role. The returned data represents
        /// events that have not yet been approved and may require administrative review.</remarks>
        /// <returns>An <see cref="IActionResult"/> containing the collection of pending events. The result is returned with an
        /// HTTP 200 status code.</returns>
        // ================= ADMIN: PENDING EVENTS =================
        [Authorize(Roles = "ADMIN")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingEvents()
        {
            return Ok(await _service.GetPendingEventsAsync());
        }


        /// <summary>
        /// Retrieves a list of events that have been rejected and are accessible only to users with the ADMIN role.
        /// </summary>
        /// <remarks>This endpoint is restricted to users with the ADMIN role. Use this method to review
        /// or manage events that have been marked as rejected.</remarks>
        /// <returns>An IActionResult containing the collection of rejected events. The result is an HTTP 200 response with the
        /// list of rejected events if successful.</returns>
        // ================= ADMIN: REJECTED EVENTS =================
        [Authorize(Roles = "ADMIN")]
        [HttpGet("rejected")]
        public async Task<IActionResult> GetRejectedEvents()
        {
            return Ok(await _service.GetRejectedEventsAsync());
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet("approved")]
        public async Task<IActionResult> GetApprovedEvents()
        {
            return Ok(await _service.GetApprovedEventsAsync());
        }

        // ================= EXPIRED EVENTS =================
        [Authorize(Roles = "ADMIN")]
        [HttpGet("expired")]
        public async Task<IActionResult> GetExpiredEvents()
        {
            return Ok(await _service.GetExpiredEventsAsync());
        }
    }
}