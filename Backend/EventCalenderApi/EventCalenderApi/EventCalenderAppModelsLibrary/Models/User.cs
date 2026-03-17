using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models
{
    public class User : IComparable<User>, IEquatable<User>
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.USER;
        public AccountStatus Status { get; set; } = AccountStatus.ACTIVE;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public List<Event> EventsCreated { get; set; } = new();
        public List<Event> EventsApproved { get; set; } = new();
        public List<EventRegistration> Registrations { get; set; } = new();
        public List<Reminder> Reminders { get; set; } = new();
        public List<Note> Notes { get; set; } = new();
        public List<Todo> Todos { get; set; } = new();

        public int CompareTo(User? other)
        {
            return other != null ? UserId.CompareTo(other.UserId) : 1;
        }

        public bool Equals(User? other)
        {
            return other != null && UserId == other.UserId;
        }

        public override string ToString()
        {
            return $"UserId: {UserId}, Name: {Name}, Email: {Email}, Role: {Role}, Status: {Status}";
        }
    }
}