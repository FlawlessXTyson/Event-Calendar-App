using System;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Event
{
    public class CreateEventRequestDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string Location { get; set; } = string.Empty;

        public EventCategory Category { get; set; }
        public EventVisibility Visibility { get; set; }

        public int CreatedByUserId { get; set; }

        public int? SeatsLimit { get; set; }
        public DateTime? RegistrationDeadline { get; set; }

        public bool IsPaidEvent { get; set; }
        public float TicketPrice { get; set; }
    }
}