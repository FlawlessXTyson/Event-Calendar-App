using EventCalenderApi.EventCalenderAppDataLibrary;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.Interfaces;

namespace EventCalenderApi.Repositories
{
    public class AuditLogRepository : Repository<int, AuditLog>, IAuditLogRepository
    {
        public AuditLogRepository(EventCalendarDbContext context) : base(context)
        {
        }
    }
}

