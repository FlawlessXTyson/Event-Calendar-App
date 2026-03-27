using System;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models
{
    public enum RefundRequestStatus { PENDING = 1, APPROVED = 2, REJECTED = 3 }

    public class RefundRequest
    {
        public int RefundRequestId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int EventId { get; set; }
        public Event? Event { get; set; }
        public int PaymentId { get; set; }
        public Payment? Payment { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public RefundRequestStatus Status { get; set; } = RefundRequestStatus.PENDING;
        public float? ApprovedPercentage { get; set; }
        public int? ReviewedByAdminId { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
