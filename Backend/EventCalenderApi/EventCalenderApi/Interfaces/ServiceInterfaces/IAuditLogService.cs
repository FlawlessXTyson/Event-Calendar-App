using EventCalenderApi.EventCalenderAppModelsLibrary.Models;

namespace EventCalenderApi.Interfaces.ServiceInterfaces
{
    public interface IAuditLogService
    {
        Task<IEnumerable<AuditLog>> GetAllAsync();
        Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId);
        Task<IEnumerable<AuditLog>> GetByEntityAsync(string entity);
        Task<IEnumerable<AuditLog>> GetByActionAsync(string action);
    }
}
