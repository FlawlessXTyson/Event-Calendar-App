using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.AuditLog;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.EntityFrameworkCore;

namespace EventCalenderApi.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _repo;
        private readonly IRepository<int, User> _userRepo;

        public AuditLogService(IAuditLogRepository repo, IRepository<int, User> userRepo)
        {
            _repo = repo;
            _userRepo = userRepo;
        }

        public async Task<IEnumerable<AuditLogResponseDTO>> GetAllAsync()
        {
            return await BuildQuery(_repo.GetQueryable());
        }

        public async Task<IEnumerable<AuditLogResponseDTO>> GetByUserIdAsync(int userId)
        {
            return await BuildQuery(_repo.GetQueryable().Where(a => a.UserId == userId));
        }

        public async Task<IEnumerable<AuditLogResponseDTO>> GetByEntityAsync(string entity)
        {
            return await BuildQuery(_repo.GetQueryable().Where(a => a.Entity.ToLower() == entity.ToLower()));
        }

        public async Task<IEnumerable<AuditLogResponseDTO>> GetByActionAsync(string action)
        {
            return await BuildQuery(_repo.GetQueryable().Where(a => a.Action.ToUpper() == action.ToUpper()));
        }

        private async Task<IEnumerable<AuditLogResponseDTO>> BuildQuery(IQueryable<AuditLog> query)
        {
            var logs = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
            var userIds = logs.Select(l => l.UserId).Distinct().ToList();
            var users = await _userRepo.GetQueryable()
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.Name);

            return logs.Select(a => new AuditLogResponseDTO
            {
                Id = a.Id,
                UserId = a.UserId,
                UserName = users.TryGetValue(a.UserId, out var name) ? name : string.Empty,
                Role = a.Role,
                Action = a.Action,
                Entity = a.Entity,
                EntityId = a.EntityId,
                CreatedAt = a.CreatedAt
            });
        }
    }
}
