using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Note;

public interface INoteService
{
    Task<CreateNoteResponseDTO> CreateAsync(CreateNoteRequestDTO dto);
    Task<IEnumerable<CreateNoteResponseDTO>> GetByUserAsync(int userId);
    Task DeleteAsync(int noteId);
}