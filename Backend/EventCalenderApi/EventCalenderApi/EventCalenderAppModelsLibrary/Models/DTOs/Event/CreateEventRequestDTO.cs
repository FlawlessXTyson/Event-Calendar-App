using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.Enums;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Event
{
    public class CreateEventRequestDTO
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        // 
        public DateTime? EventEndDate { get; set; }

        [Required]
        public TimeSpan? StartTime { get; set; }

        [Required]
        public TimeSpan? EndTime { get; set; }

        public string Location { get; set; } = string.Empty;

        [JsonIgnore]
        public EventVisibility Visibility { get; set; }

        [JsonIgnore]
        public int CreatedByUserId { get; set; }

        public int? SeatsLimit { get; set; }

        public DateTime? RegistrationDeadline { get; set; }

        public bool IsPaidEvent { get; set; }

        public float TicketPrice { get; set; }
    }
}