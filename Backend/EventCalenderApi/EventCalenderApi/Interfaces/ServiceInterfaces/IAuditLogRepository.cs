using EventCalenderApi.EventCalenderAppModelsLibrary.Models;

namespace EventCalenderApi.Interfaces
{
    public interface IAuditLogRepository : IRepository<int, AuditLog>
    {
    }
}