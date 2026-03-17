namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Note
{
    public class CreateNoteResponseDTO
    {
        public int NoteId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}