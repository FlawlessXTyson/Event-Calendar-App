using System.ComponentModel.DataAnnotations;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Payment
{
    public class PaymentRequestDTO
    {
        [Required]
        public int EventId { get; set; }
    }
}