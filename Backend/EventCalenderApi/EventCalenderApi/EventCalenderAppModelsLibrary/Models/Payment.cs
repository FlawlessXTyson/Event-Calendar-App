using System;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models
{
    public class Payment : IComparable<Payment>, IEquatable<Payment>
    {
        public int PaymentId { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int EventId { get; set; }
        public Event? Event { get; set; }

        public float AmountPaid { get; set; }
        public float CommissionAmount { get; set; }
        public float OrganizerAmount { get; set; }

        public float? RefundedAmount { get; set; }
        public DateTime? RefundedAt { get; set; }
        public string? CancelledBy { get; set; }  // "USER", "ADMIN", "ORGANIZER"

        public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public int CompareTo(Payment? other)
        {
            return other != null ? PaymentId.CompareTo(other.PaymentId) : 1;
        }

        public bool Equals(Payment? other)
        {
            return other != null && PaymentId == other.PaymentId;
        }

        public override string ToString()
        {
            return $"PaymentId: {PaymentId}, UserId: {UserId}, EventId: {EventId}, Amount: {AmountPaid}, Status: {Status}";
        }
    }
}