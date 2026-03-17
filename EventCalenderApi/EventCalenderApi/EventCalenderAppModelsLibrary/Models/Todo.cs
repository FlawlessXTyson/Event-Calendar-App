using System;
using System.ComponentModel.DataAnnotations;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models
{
    public class Todo : IComparable<Todo>, IEquatable<Todo>
    {
        public int TodoId { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public string TaskTitle { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }

        public TodoStatus Status { get; set; } = TodoStatus.PENDING;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int CompareTo(Todo? other)
        {
            return other != null ? TodoId.CompareTo(other.TodoId) : 1;
        }

        public bool Equals(Todo? other)
        {
            return other != null && TodoId == other.TodoId;
        }

        public override string ToString()
        {
            return $"TodoId: {TodoId}, Task: {TaskTitle}, DueDate: {DueDate}, Status: {Status}, UserId: {UserId}";
        }
    }
}