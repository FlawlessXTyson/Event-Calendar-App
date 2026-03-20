using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Note;

namespace EventCalenderApi.Interfaces.ServiceInterfaces
{
    public interface INoteService
    {
        Task<CreateNoteResponseDTO> CreateAsync(CreateNoteRequestDTO dto);

        Task<IEnumerable<CreateNoteResponseDTO>> GetByUserAsync(int userId);

        Task DeleteAsync(int noteId, int userId);
    }
}