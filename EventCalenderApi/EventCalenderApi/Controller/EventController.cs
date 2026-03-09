using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Event;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventCalenderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All endpoints require login by default
    public class EventController : ControllerBase
    {
        private readonly IEventService _service;

        public EventController(IEventService service)
        {
            _service = service;
        }

        // =====================================
        // CREATE EVENT (Organizer or Admin)
        // =====================================
        [Authorize(Roles = "ORGANIZER,ADMIN")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateEventRequestDTO dto)
        {
            var result = await _service.CreateEventAsync(dto);
            return Ok(result);
        }

        // =====================================
        // GET ALL EVENTS (Public)
        // =====================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        // =====================================
        // GET EVENT BY ID
        // =====================================
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound("Event not found");

            return Ok(result);
        }

        // =====================================
        // DELETE EVENT (Admin Only)
        // =====================================
        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);

            if (result == null)
                return NotFound("Event not found");

            return Ok(result);
        }

        // =====================================
        // APPROVE EVENT (Admin Only)
        // =====================================
        [Authorize(Roles = "ADMIN")]
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _service.ApproveAsync(id, adminId);

            if (result == null)
                return NotFound("Event not found");

            return Ok(result);
        }

        // =====================================
        // REJECT EVENT (Admin Only)
        // =====================================
        [Authorize(Roles = "ADMIN")]
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(int id)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _service.RejectAsync(id, adminId);

            if (result == null)
                return NotFound("Event not found");

            return Ok(result);
        }

        // =====================================
        // CANCEL EVENT (Admin Only)
        // =====================================
        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _service.CancelEventAsync(id);

            if (result == null)
                return NotFound("Event not found");

            return Ok(result);
        }

        // =====================================
        // SEARCH EVENTS
        // =====================================
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search(string keyword)
        {
            var result = await _service.SearchAsync(keyword);
            return Ok(result);
        }

        // =====================================
        // GET EVENTS BY DATE RANGE
        // =====================================
        [HttpGet("range")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByDateRange(DateTime start, DateTime end)
        {
            var result = await _service.GetByDateRangeAsync(start, end);
            return Ok(result);
        }

        // =====================================
        // PAGINATION
        // =====================================
        [HttpGet("paged")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPaged(int pageNumber = 1, int pageSize = 5)
        {
            var result = await _service.GetPagedAsync(pageNumber, pageSize);
            return Ok(result);
        }
    }
}