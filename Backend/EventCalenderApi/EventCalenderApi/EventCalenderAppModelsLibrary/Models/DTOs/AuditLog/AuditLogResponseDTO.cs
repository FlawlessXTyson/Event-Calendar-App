using System;

namespace EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.AuditLog
{
    public class AuditLogResponseDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
