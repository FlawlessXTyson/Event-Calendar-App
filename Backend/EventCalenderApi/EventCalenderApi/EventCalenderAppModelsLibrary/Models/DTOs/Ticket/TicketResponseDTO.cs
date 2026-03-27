using System;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Ticket
{
    public class TicketResponseDTO
    {
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public string EventDescription { get; set; } = string.Empty;
        public string EventLocation { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public int? PaymentId { get; set; }
        public float AmountPaid { get; set; }
        public bool IsPaidEvent { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
