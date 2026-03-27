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

        public string? StartTime { get; set; }  // "HH:mm" — for deadline check on frontend
        public string? EndTime { get; set; }    // "HH:mm"

        public EventCategory Category { get; set; }
        public EventVisibility Visibility { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; }

        public bool IsPaidEvent { get; set; }
        public float TicketPrice { get; set; }

        public int SeatsLeft { get; set; }

        public string OrganizerName { get; set; } = string.Empty;

        public int RefundCutoffDays { get; set; }
        public float EarlyRefundPercentage { get; set; }
        public DateTime? RegistrationDeadline { get; set; }

        /// <summary>
        /// Computed server-side: true when registration is still open.
        /// False when: deadline passed, event already started, or event ended.
        /// Frontend should use this instead of doing its own time math.
        /// </summary>
        public bool IsRegistrationOpen { get; set; }
    }
}