using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventCalenderApi.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notifSvc;

        public NotificationController(INotificationService notifSvc)
        {
            _notifSvc = notifSvc;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /api/Notification
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var result = await _notifSvc.GetUserNotificationsAsync(CurrentUserId);
            return Ok(result);
        }

        // GET /api/Notification/unread-count
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var count = await _notifSvc.GetUnreadCountAsync(CurrentUserId);
            return Ok(new { count });
        }

        // PUT /api/Notification/{id}/read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notifSvc.MarkAsReadAsync(id, CurrentUserId);
            return Ok(new { message = "Marked as read" });
        }

        // PUT /api/Notification/read-all
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            await _notifSvc.MarkAllAsReadAsync(CurrentUserId);
            return Ok(new { message = "All notifications marked as read" });
        }
    }
}
