using System;
using System.ComponentModel.DataAnnotations;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models
{
    public class Note : IComparable<Note>, IEquatable<Note>
    {
        public int NoteId { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int CompareTo(Note? other)
        {
            return other != null ? NoteId.CompareTo(other.NoteId) : 1;
        }

        public bool Equals(Note? other)
        {
            return other != null && NoteId == other.NoteId;
        }

        public override string ToString()
        {
            return $"NoteId: {NoteId}, Title: {Title}, UserId: {UserId}, CreatedAt: {CreatedAt}";
        }
    }
}