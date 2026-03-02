using System;
using System.ComponentModel.DataAnnotations;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models
{
    public class EventRegistration : IComparable<EventRegistration>, IEquatable<EventRegistration>
    {
        public int RegistrationId { get; set; }
        public int EventId { get; set; }
        public Event? Event { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public RegistrationStatus Status { get; set; } = RegistrationStatus.REGISTERED;
        public DateTime RegisteredAt { get; set; } = DateTime.Now;

        public int CompareTo(EventRegistration? other)
        {
            return other != null ? RegistrationId.CompareTo(other.RegistrationId) : 1;
        }

        public bool Equals(EventRegistration? other)
        {
            return other != null && RegistrationId == other.RegistrationId;
        }

        public override string ToString()
        {
            return $"RegistrationId: {RegistrationId}, EventId: {EventId}, UserId: {UserId}, Status: {Status}, RegisteredAt: {RegisteredAt}";
        }
    }
}