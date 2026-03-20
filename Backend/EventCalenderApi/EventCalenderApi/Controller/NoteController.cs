using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Note;
using EventCalenderApi.Interfaces.ServiceInterfaces;

namespace EventCalenderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NoteController : ControllerBase
    {
        private readonly INoteService _service;

        public NoteController(INoteService service)
        {
            _service = service;
        }

        // create note
        [HttpPost]
        public async Task<IActionResult> Create(CreateNoteRequestDTO dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            dto.UserId = userId;

            return Ok(await _service.CreateAsync(dto));
        }

        // get my notes
        [HttpGet("me")]
        public async Task<IActionResult> GetMyNotes()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            return Ok(await _service.GetByUserAsync(userId));
        }

        // delete my note
        [HttpDelete("{noteId}")]
        public async Task<IActionResult> Delete(int noteId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            await _service.DeleteAsync(noteId, userId);

            return NoContent();
        }
    }
}