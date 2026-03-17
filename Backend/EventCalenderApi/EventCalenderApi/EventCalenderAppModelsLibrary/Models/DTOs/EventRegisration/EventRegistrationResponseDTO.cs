using System;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.EventRegisration
{
    public class EventRegistrationResponseDTO
    {
        public int RegistrationId { get; set; }
        public int EventId { get; set; }
        public int UserId { get; set; }
        public RegistrationStatus Status { get; set; }
        public DateTime RegisteredAt { get; set; }
    }
}