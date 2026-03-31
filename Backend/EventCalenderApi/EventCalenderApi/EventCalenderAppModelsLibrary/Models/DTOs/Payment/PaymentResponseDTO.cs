using System;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Payment
{
    public class PaymentResponseDTO
    {
        public int PaymentId { get; set; }
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public DateTime? EventDate { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;

        public float AmountPaid { get; set; }

        public float OrganizerAmount { get; set; }

        public float? RefundedAmount { get; set; }

        public PaymentStatus Status { get; set; }

        public DateTime PaymentDate { get; set; }

        public DateTime? RefundedAt { get; set; }

        public string? CancelledBy { get; set; }  // "USER", "ADMIN", "ORGANIZER"
    }
}