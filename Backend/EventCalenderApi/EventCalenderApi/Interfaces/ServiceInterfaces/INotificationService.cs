using EventCalenderApi.EventCalenderAppModelsLibrary.Models;
using EventCalenderApi.EventCalenderAppModelsLibrary.Models.DTOs.Notification;

namespace EventCalenderApi.Interfaces.ServiceInterfaces
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(int userId, string title, string message, NotificationType type);
        Task<IEnumerable<NotificationResponseDTO>> GetUserNotificationsAsync(int userId);
        Task MarkAsReadAsync(int notificationId, int userId);
        Task MarkAllAsReadAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
    }
}
