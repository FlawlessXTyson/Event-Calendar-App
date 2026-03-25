using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventCalenderApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ADMIN")]
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        // GET /api/AuditLog — all logs, newest first
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _auditLogService.GetAllAsync());
        }

        // GET /api/AuditLog/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            return Ok(await _auditLogService.GetByUserIdAsync(userId));
        }

        // GET /api/AuditLog/entity/{entity}
        [HttpGet("entity/{entity}")]
        public async Task<IActionResult> GetByEntity(string entity)
        {
            return Ok(await _auditLogService.GetByEntityAsync(entity));
        }

        // GET /api/AuditLog/action/{action}
        [HttpGet("action/{action}")]
        public async Task<IActionResult> GetByAction(string action)
        {
            return Ok(await _auditLogService.GetByActionAsync(action));
        }
    }
}
