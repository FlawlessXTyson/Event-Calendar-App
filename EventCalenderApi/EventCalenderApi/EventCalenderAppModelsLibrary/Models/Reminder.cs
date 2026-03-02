using System;
using System.ComponentModel.DataAnnotations;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models
{
    public class Reminder : IComparable<Reminder>, IEquatable<Reminder>
    {
        public int ReminderId { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        // Reminder can be linked to an event OR independent
        public int? EventId { get; set; }
        public Event? Event { get; set; }

        public string ReminderTitle { get; set; } = string.Empty;
        public DateTime ReminderDateTime { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int CompareTo(Reminder? other)
        {
            return other != null ? ReminderId.CompareTo(other.ReminderId) : 1;
        }

        public bool Equals(Reminder? other)
        {
            return other != null && ReminderId == other.ReminderId;
        }

        public override string ToString()
        {
            return $"ReminderId: {ReminderId}, Title: {ReminderTitle}, DateTime: {ReminderDateTime}, UserId: {UserId}, EventId: {EventId}";
        }
    }
}