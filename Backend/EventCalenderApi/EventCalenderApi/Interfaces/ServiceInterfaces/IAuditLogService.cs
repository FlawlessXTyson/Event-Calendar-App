using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.AuditLog;

namespace EventCalenderApi.Interfaces.ServiceInterfaces
{
    public interface IAuditLogService
    {
        Task<IEnumerable<AuditLogResponseDTO>> GetAllAsync();
        Task<IEnumerable<AuditLogResponseDTO>> GetByUserIdAsync(int userId);
        Task<IEnumerable<AuditLogResponseDTO>> GetByEntityAsync(string entity);
        Task<IEnumerable<AuditLogResponseDTO>> GetByActionAsync(string action);
    }
}
