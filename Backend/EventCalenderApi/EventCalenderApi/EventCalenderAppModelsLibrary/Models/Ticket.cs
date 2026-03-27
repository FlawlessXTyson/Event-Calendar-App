using System;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models
{
    public class Ticket
    {
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int EventId { get; set; }
        public Event? Event { get; set; }
        public int? PaymentId { get; set; }
        public Payment? Payment { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
