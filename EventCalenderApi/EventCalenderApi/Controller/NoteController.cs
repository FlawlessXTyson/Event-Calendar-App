using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Note;
using EventCalenderApi.Interfaces.ServiceInterfaces;

namespace EventCalenderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Login required
    public class NoteController : ControllerBase
    {
        private readonly INoteService _service;

        public NoteController(INoteService service)
        {
            _service = service;
        }


        //create note ~~ loggedin user only
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNoteRequestDTO dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            dto.UserId = userId;

            var result = await _service.CreateAsync(dto);

            return Ok(result);
        }


        //get my notes
        [HttpGet("me")]
        public async Task<IActionResult> GetMyNotes()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _service.GetByUserAsync(userId);

            return Ok(result);
        }


        //delete my note
        [HttpDelete("{noteId}")]
        public async Task<IActionResult> Delete(int noteId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var notes = await _service.GetByUserAsync(userId);

            if (!notes.Any(n => n.NoteId == noteId))
                return Forbid("You can delete only your own notes.");

            await _service.DeleteAsync(noteId);

            return NoContent();
        }
    }
}