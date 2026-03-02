namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Remainder
{
    public class CreateReminderRequestDTO
    {
        public int UserId { get; set; }
        public int? EventId { get; set; }
        public string ReminderTitle { get; set; } = string.Empty;
        public DateTime ReminderDateTime { get; set; }
    }
}