using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Note;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;

public class NoteService : INoteService
{
    private readonly IRepository<int, Note> _repo;

    public NoteService(IRepository<int, Note> repo)
    {
        _repo = repo;
    }

    public async Task<CreateNoteResponseDTO> CreateAsync(CreateNoteRequestDTO dto)
    {
        var note = new Note
        {
            UserId = dto.UserId,
            Title = dto.Title,
            Content = dto.Content,
            CreatedAt = DateTime.Now
        };

        var created = await _repo.AddAsync(note);

        return new CreateNoteResponseDTO
        {
            NoteId = created.NoteId,
            UserId = created.UserId,
            Title = created.Title,
            Content = created.Content,
            CreatedAt = created.CreatedAt
        };
    }

    public async Task<IEnumerable<CreateNoteResponseDTO>> GetByUserAsync(int userId)
    {
        var notes = await _repo.GetAllAsync();

        return notes
            .Where(n => n.UserId == userId)
            .Select(n => new CreateNoteResponseDTO
            {
                NoteId = n.NoteId,
                UserId = n.UserId,
                Title = n.Title,
                Content = n.Content,
                CreatedAt = n.CreatedAt
            });
    }

    public async Task DeleteAsync(int noteId)
    {
        var note = await _repo.GetByIdAsync(noteId);

        if (note == null)
            throw new NotFoundException("Note not found");

        await _repo.DeleteAsync(noteId);
    }
}