using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Notification;
using EventCalenderApi.Exceptions;
using EventCalenderApi.Interfaces;
using EventCalenderApi.Interfaces.ServiceInterfaces;
using Microsoft.EntityFrameworkCore;

namespace EventCalenderApi.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IRepository<int, Notification> _notifRepo;

        public NotificationService(IRepository<int, Notification> notifRepo)
        {
            _notifRepo = notifRepo;
        }

        public async Task CreateNotificationAsync(int userId, string title, string message, NotificationType type)
        {
            await _notifRepo.AddAsync(new Notification
            {
                UserId    = userId,
                Title     = title,
                Message   = message,
                Type      = type,
                IsRead    = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        public async Task<IEnumerable<NotificationResponseDTO>> GetUserNotificationsAsync(int userId)
        {
            var list = await _notifRepo.GetQueryable()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            return list.Select(Map);
        }

        public async Task MarkAsReadAsync(int notificationId, int userId)
        {
            var notif = await _notifRepo.GetByIdAsync(notificationId)
                ?? throw new NotFoundException("Notification not found");

            if (notif.UserId != userId)
                throw new UnauthorizedException("Access denied");

            notif.IsRead = true;
            await _notifRepo.UpdateAsync(notificationId, notif);
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var unread = await _notifRepo.GetQueryable()
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
                await _notifRepo.UpdateAsync(n.NotificationId, n);
            }
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _notifRepo.GetQueryable()
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        private static NotificationResponseDTO Map(Notification n) => new()
        {
            NotificationId = n.NotificationId,
            UserId         = n.UserId,
            Title          = n.Title,
            Message        = n.Message,
            Type           = n.Type.ToString(),
            IsRead         = n.IsRead,
            CreatedAt      = n.CreatedAt
        };
    }
}
