using System;
using System.Collections.Generic;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models
{
    public class Event : IComparable<Event>, IEquatable<Event>
    {
        public int EventId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }

        public string Location { get; set; } = string.Empty;

        public EventCategory Category { get; set; } = EventCategory.PUBLIC;
        public EventVisibility Visibility { get; set; } = EventVisibility.PUBLIC;

        // Creator (Organizer/Admin/User)
        public int CreatedByUserId { get; set; }
        public User? CreatedBy { get; set; }

        // Admin approval
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.PENDING;

        public int? ApprovedByUserId { get; set; }
        public User? ApprovedBy { get; set; }

        // Optional controls
        public int? SeatsLimit { get; set; }
        public DateTime? RegistrationDeadline { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Payment related
        public bool IsPaidEvent { get; set; } = false;
        public float TicketPrice { get; set; } = 0;
        public float CommissionPercentage { get; set; } = 10; // default 10%

        // Navigation
        public List<EventRegistration> Registrations { get; set; } = new();
        public List <Payment> Payments { get; set; } = new();

        public int CompareTo(Event? other)
        {
            return other != null ? EventId.CompareTo(other.EventId) : 1;
        }

        public bool Equals(Event? other)
        {
            return other != null && EventId == other.EventId;
        }

        public override string ToString()
        {
            return $"EventId: {EventId}, Title: {Title}, Date: {EventDate:yyyy-MM-dd}, Category: {Category}, Approval: {ApprovalStatus}";
        }
    }
}