using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Event;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventCalenderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All endpoints require login
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
            return Ok(await _service.CreateEventAsync(dto));
        }

        // =====================================
        // GET ALL EVENTS (Any Logged User)
        // =====================================
        [HttpGet]
        [AllowAnonymous] // Optional: Allow public viewing
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        // =====================================
        // GET BY ID
        // =====================================
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Get(int id)
        {
            var result = await _service.GetByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        // =====================================
        // DELETE EVENT (Admin Only)
        // =====================================
        [Authorize(Roles = "ADMIN")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            return result == null ? NotFound() : Ok(result);
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

            return result == null ? NotFound() : Ok(result);
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

            return result == null ? NotFound() : Ok(result);
        }
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> Search(string keyword)
        {
            return Ok(await _service.SearchAsync(keyword));
        }
        [HttpGet("range")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByDateRange(DateTime start, DateTime end)
        {
            return Ok(await _service.GetByDateRangeAsync(start, end));
        }
        [HttpGet("paged")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPaged(int pageNumber = 1, int pageSize = 5)
        {
            return Ok(await _service.GetPagedAsync(pageNumber, pageSize));
        }
    }
}