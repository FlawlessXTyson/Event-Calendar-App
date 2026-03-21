namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Todo
{
    public class UpdateTodoRequestDTO
    {
        public string TaskTitle { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
    }
}