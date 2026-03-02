using System;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Event
{
    public class EventResponseDTO
    {
        public int EventId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;

        public EventCategory Category { get; set; }
        public EventVisibility Visibility { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; }

        public bool IsPaidEvent { get; set; }
        public float TicketPrice { get; set; }
    }
}