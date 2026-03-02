namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Note
{
    public class CreateNoteRequestDTO
    {
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}