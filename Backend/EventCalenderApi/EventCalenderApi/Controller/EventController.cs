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

        // ================= CREATE EVENT =================
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

        // ================= GET ALL APPROVED =================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();

            if (!result.Any())
                throw new NotFoundException("No events available");

            return Ok(result);
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id)
        {
            return Ok(await _service.GetByIdAsync(id));
        }

        // ================= DELETE =================
        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            return Ok(await _service.DeleteAsync(id));
        }

        // ================= APPROVE =================
        [Authorize(Roles = "ADMIN")]
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            return Ok(await _service.ApproveAsync(id, adminId));
        }

        // ================= REJECT =================
        [Authorize(Roles = "ADMIN")]
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(int id)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            return Ok(await _service.RejectAsync(id, adminId));
        }

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

        // ================= REFUND SUMMARY =================
        [Authorize(Roles = "ADMIN,ORGANIZER")]
        [HttpGet("{id}/refund-summary")]
        public async Task<IActionResult> GetRefundSummary(int id)
        {
            return Ok(await _service.GetRefundSummaryAsync(id));
        }

        // ================= SEARCH =================
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search(string keyword)
        {
            var result = await _service.SearchAsync(keyword);

            if (!result.Any())
                throw new NotFoundException("No events found");

            return Ok(result);
        }

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

        // ================= PAGINATION =================
        [HttpGet("paged")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPaged(int pageNumber = 1, int pageSize = 5)
        {
            return Ok(await _service.GetPagedAsync(pageNumber, pageSize));
        }

        // ================= MY EVENTS =================
        [Authorize(Roles = "ORGANIZER,ADMIN")]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyEvents()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _service.GetMyEventsAsync(userId);

            if (!result.Any())
                throw new NotFoundException("No events created");

            return Ok(result);
        }

        // ================= USER REGISTERED EVENTS =================
        [Authorize(Roles = "USER")]
        [HttpGet("registered")]
        public async Task<IActionResult> GetRegisteredEvents()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            return Ok(await _service.GetRegisteredEventsAsync(userId));
        }

        // ================= ADMIN: PENDING EVENTS =================
        [Authorize(Roles = "ADMIN")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingEvents()
        {
            return Ok(await _service.GetPendingEventsAsync());
        }

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
            var result = await _service.GetApprovedEventsAsync();

            if (!result.Any())
                throw new NotFoundException("No approved events found");

            return Ok(result);
        }
    }
}