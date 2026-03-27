using System;
using System.Collections.Generic;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models
{
    public class Event : IComparable<Event>, IEquatable<Event>
    {
        public int EventId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        //  START DATE
        public DateTime EventDate { get; set; }

        //  NEW: END DATE 
        public DateTime? EventEndDate { get; set; }

        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }

        public string Location { get; set; } = string.Empty;

        public EventCategory Category { get; set; } = EventCategory.PUBLIC;
        public EventVisibility Visibility { get; set; } = EventVisibility.PUBLIC;

        public EventStatus Status { get; set; } = EventStatus.ACTIVE;

        public int CreatedByUserId { get; set; }
        public User? CreatedBy { get; set; }//navigationnnnn to user 

        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.PENDING;

        public int? ApprovedByUserId { get; set; }// ? cos not not aprroved yet 
        public User? ApprovedBy { get; set; }

        public int? SeatsLimit { get; set; }
        public DateTime? RegistrationDeadline { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;// auto sets the datew

        public bool IsPaidEvent { get; set; } = false;
        public float TicketPrice { get; set; } = 0;
        public float CommissionPercentage { get; set; } = 10;

        // Refund policy: % refunded if user cancels >= RefundCutoffDays before event
        // If cancelled within RefundCutoffDays → 0% refund (no refund)
        // Default: 100% refund if cancelled 2+ days before, 0% within 2 days
        public int RefundCutoffDays { get; set; } = 2;
        public float EarlyRefundPercentage { get; set; } = 100f;

        public List<EventRegistration> Registrations { get; set; } = new();
        public List<Payment> Payments { get; set; } = new();

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
            return $"EventId: {EventId}, Title: {Title}, Date: {EventDate:yyyy-MM-dd}, Approval: {ApprovalStatus}";
        }
    }
}