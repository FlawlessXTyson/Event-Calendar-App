using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.EntityFrameworkCore;

namespace EventCalenderApi.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _repo;

        public AuditLogService(IAuditLogRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<AuditLog>> GetAllAsync()
        {
            return await _repo.GetQueryable()
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(int userId)
        {
            return await _repo.GetQueryable()
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entity)
        {
            return await _repo.GetQueryable()
                .Where(a => a.Entity.ToLower() == entity.ToLower())
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetByActionAsync(string action)
        {
            return await _repo.GetQueryable()
                .Where(a => a.Action.ToUpper() == action.ToUpper())
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
    }
}
