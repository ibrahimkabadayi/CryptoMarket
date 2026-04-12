using AutoMapper;
using Notifications.API.Application.DTOs;
using Notifications.API.Application.Interfaces;
using Notifications.API.Domain.Entities;
using Notifications.API.Domain.Enums;
using Notifications.API.Domain.Interfaces;

namespace Notifications.API.Application.Services;

public class NotificationService(INotificationRepository notificationRepository, IMapper mapper) : INotificationService
{
    public async Task CreateNotificationAsync(Guid userId, string title, string message, NotificationType type, string? relatedEntityId = null)
    {
        var notification = new Notification(userId, title, message, type, relatedEntityId);

        await notificationRepository.AddAsync(notification);
    }

    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId)
    {
        var notifications = await notificationRepository.FindAsync(n => n.UserId == userId);
        var orderedNotifications = notifications.OrderByDescending(n => n.Id).ToList();

        return mapper.Map<IEnumerable<NotificationDto>>(orderedNotifications);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        var unreadNotifications = await notificationRepository.FindAsync(n => n.UserId == userId && !n.IsRead);
        return unreadNotifications.Count();
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await notificationRepository.GetByIdAsync(notificationId);

        if (notification == null || notification.UserId != userId)
        {
            throw new Exception("Bildirim bulunamadı veya yetkisiz erişim.");
        }

        notification.MarkAsRead();

        await notificationRepository.UpdateAsync(notification);
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var unreadNotifications = await notificationRepository.FindAsync(n => n.UserId == userId && !n.IsRead);

        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead();
            await notificationRepository.UpdateAsync(notification);
        }
    }
}
