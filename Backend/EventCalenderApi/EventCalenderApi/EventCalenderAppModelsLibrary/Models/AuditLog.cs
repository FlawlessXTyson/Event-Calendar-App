using System;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string Role { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string Entity { get; set; } = string.Empty;

        public int EntityId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}