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

        //create event (Organizer and Admin only)
        [Authorize(Roles = "ORGANIZER,ADMIN")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEventRequestDTO dto)
        {
            var result = await _service.CreateEventAsync(dto);
            return Ok(result);
        }

        //get all events (public)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        //get event by id (public)
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound("Event not found");

            return Ok(result);
        }


        //delete event (admin only)
        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);

            if (result == null)
                return NotFound("Event not found");

            return Ok(result);
        }


        //approve event (admin only)
        [Authorize(Roles = "ADMIN")]
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _service.ApproveAsync(id, adminId);

            return Ok(result);
        }


        //reject event (admin only)
        [Authorize(Roles = "ADMIN")]
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(int id)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _service.RejectAsync(id, adminId);

            return Ok(result);
        }


        //cancel event (admin only)
        [Authorize(Roles = "ADMIN")]
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _service.CancelEventAsync(id);

            return Ok(result);
        }


        //search events (public)
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest("Keyword is required.");

            var result = await _service.SearchAsync(keyword);

            return Ok(result);
        }


        //get events within date range
        [HttpGet("range")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByDateRange(DateTime start, DateTime end)
        {
            if (start > end)
                return BadRequest("Start date must be before end date.");

            var result = await _service.GetByDateRangeAsync(start, end);

            return Ok(result);
        }


        //pagination
        [HttpGet("paged")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPaged(int pageNumber = 1, int pageSize = 5)
        {
            var result = await _service.GetPagedAsync(pageNumber, pageSize);

            return Ok(result);
        }
    }
}