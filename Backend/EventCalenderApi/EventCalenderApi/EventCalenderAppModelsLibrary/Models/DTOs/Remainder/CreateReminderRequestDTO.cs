namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Remainder
{
    public class CreateReminderRequestDTO
    {
        //public int UserId { get; set; }   //removed

        public int? EventId { get; set; }

        public string ReminderTitle { get; set; } = string.Empty;

        //manual
        public DateTime? ReminderDateTime { get; set; }

        //auto
        public int? MinutesBefore { get; set; }
    }
}