using System;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Payment
{
    public class PaymentResponseDTO
    {
        public int PaymentId { get; set; }
        public int EventId { get; set; }
        public float AmountPaid { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}