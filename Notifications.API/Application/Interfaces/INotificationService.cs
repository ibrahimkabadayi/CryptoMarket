using Notifications.API.Application.DTOs;
using Notifications.API.Domain.Enums;

namespace Notifications.API.Application.Interfaces;

public interface INotificationService
{
    Task CreateNotificationAsync(Guid userId, string title, string message, NotificationType type, string? relatedEntityId = null);
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task MarkAsReadAsync(Guid notificationId, Guid userId);
    Task MarkAllAsReadAsync(Guid userId);
}
