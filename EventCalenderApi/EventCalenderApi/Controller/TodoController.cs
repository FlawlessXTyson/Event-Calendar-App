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

        //create todo
        [HttpPost]
        public async Task<IActionResult> Create(CreateTodoRequestDTO dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            dto.UserId = userId;

            return Ok(await _service.CreateAsync(dto));
        }

        //get my todos
        [HttpGet("me")]
        public async Task<IActionResult> GetMyTodos()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            return Ok(await _service.GetByUserAsync(userId));
        }

        //mark completed
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> Complete(int id)
        {
            await _service.MarkCompletedAsync(id);

            return NoContent();
        }
    }
}