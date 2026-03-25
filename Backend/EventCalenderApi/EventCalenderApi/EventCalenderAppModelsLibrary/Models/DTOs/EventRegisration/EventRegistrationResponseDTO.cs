using System;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.EventRegisration
{
    public class EventRegistrationResponseDTO
    {
        public int RegistrationId { get; set; }
        public int EventId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public RegistrationStatus Status { get; set; }
        public DateTime RegisteredAt { get; set; }
    }
}