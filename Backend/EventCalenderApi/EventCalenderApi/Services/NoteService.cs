using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Note;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.EntityFrameworkCore;

namespace EventCalenderApi.Services
{
    public class NoteService : INoteService
    {
        private readonly IRepository<int, Note> _repo;

        public NoteService(IRepository<int, Note> repo)
        {
            _repo = repo;
        }

        public async Task<CreateNoteResponseDTO> CreateAsync(CreateNoteRequestDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new BadRequestException("Title is required");

            var note = new Note
            {
                UserId = dto.UserId,
                Title = dto.Title,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repo.AddAsync(note);

            return MapToDTO(created);
        }

        public async Task<IEnumerable<CreateNoteResponseDTO>> GetByUserAsync(int userId)
        {
            var notes = await _repo
                .GetQueryable()
                .Where(n => n.UserId == userId)
                .ToListAsync();

            return notes.Select(MapToDTO);
        }

        public async Task DeleteAsync(int noteId, int userId)
        {
            var note = await _repo.GetByIdAsync(noteId);

            if (note == null)
                throw new NotFoundException("Note not found");

            if (note.UserId != userId)
                throw new UnauthorizedException("You can delete only your own notes");

            await _repo.DeleteAsync(noteId);
        }

        // mapping method (clean code)
        private static CreateNoteResponseDTO MapToDTO(Note note)
        {
            return new CreateNoteResponseDTO
            {
                NoteId = note.NoteId,
                UserId = note.UserId,
                Title = note.Title,
                Content = note.Content,
                CreatedAt = note.CreatedAt
            };
        }
    }
}