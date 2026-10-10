using Notifications.API.Application.DTOs;
using Notifications.API.Domain.Enums;

namespace Notifications.API.Application.Interfaces;

public interface INotificationService
{
    Task CreateNotificationAsync(string userId, string title, string message, NotificationType type, string? relatedEntityId = null);
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAsReadAsync(Guid notificationId, string userId);
    Task MarkAllAsReadAsync(string userId);
}
