using System;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.RefundRequest
{
    public class RefundRequestResponseDTO
    {
        public int RefundRequestId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public int PaymentId { get; set; }
        public float AmountPaid { get; set; }
        public DateTime RequestedAt { get; set; }
        public RefundRequestStatus Status { get; set; }
        public float? ApprovedPercentage { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    public class ApproveRefundDTO
    {
        public float RefundPercentage { get; set; } // 0–100
    }
}
