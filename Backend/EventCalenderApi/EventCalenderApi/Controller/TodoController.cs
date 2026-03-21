using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Todo;

namespace EventCalenderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TodoController : ControllerBase
    {
        private readonly ITodoService _service;

        public TodoController(ITodoService service)
        {
            _service = service;
        }

        // CREATE
        [HttpPost]
        public async Task<IActionResult> Create(CreateTodoRequestDTO dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            dto.UserId = userId;

            return Ok(await _service.CreateAsync(dto));
        }

        // GET
        [HttpGet("me")]
        public async Task<IActionResult> GetMyTodos()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _service.GetByUserAsync(userId));
        }

        // COMPLETE
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> Complete(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _service.MarkCompletedAsync(id, userId);
            return NoContent();
        }

        // UPDATE 
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateTodoRequestDTO dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _service.UpdateAsync(id, userId, dto));
        }

        // DELETE 
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
    }
}