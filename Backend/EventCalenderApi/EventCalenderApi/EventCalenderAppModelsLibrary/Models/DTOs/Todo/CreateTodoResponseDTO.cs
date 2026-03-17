namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Todo
{
    public class CreateTodoResponseDTO
    {
        public int TodoId { get; set; }
        public int UserId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public TodoStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}