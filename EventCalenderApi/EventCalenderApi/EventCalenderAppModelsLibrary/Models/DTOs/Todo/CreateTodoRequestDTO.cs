namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Todo
{
    public class CreateTodoRequestDTO
    {
        public int UserId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
    }
}