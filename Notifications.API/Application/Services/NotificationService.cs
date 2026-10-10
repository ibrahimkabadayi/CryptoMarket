using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Notifications.API.Application.DTOs;
using Notifications.API.Application.Interfaces;
using Notifications.API.Domain.Entities;
using Notifications.API.Domain.Enums;
using Notifications.API.Domain.Interfaces;
using Notifications.API.Hubs;

namespace Notifications.API.Application.Services;

public class NotificationService(
    INotificationRepository notificationRepository,
    IMapper mapper,
    IHubContext<NotificationHub> hubContext
    ) : INotificationService
{
    public async Task CreateNotificationAsync(string userId, string title, string message, NotificationType type, string? relatedEntityId = null)
    {
        var userGuid = Guid.Parse(userId);
        var notification = new Notification(userGuid, title, message, type, relatedEntityId);

        await notificationRepository.AddAsync(notification);

        await hubContext.Clients.User(userId.ToString())
            .SendAsync("ReceiveNotification", notification);
    }

    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(string userId)
    {
        var userGuid = Guid.Parse(userId);
        var notifications = await notificationRepository.FindAsync(n => n.UserId == userGuid);
        var orderedNotifications = notifications.OrderByDescending(n => n.Id).ToList();

        return mapper.Map<IEnumerable<NotificationDto>>(orderedNotifications);
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        var userGuid = Guid.Parse(userId);
        var unreadNotifications = await notificationRepository.FindAsync(n => n.UserId == userGuid && !n.IsRead);
        return unreadNotifications.Count;
    }

    public async Task MarkAsReadAsync(Guid notificationId, string userId)
    {
        var userGuid = Guid.Parse(userId);
        var notification = await notificationRepository.GetByIdAsync(notificationId);

        if (notification == null || notification.UserId != userGuid)
        {
            throw new Exception("Bildirim bulunamadı veya yetkisiz erişim.");
        }

        notification.MarkAsRead();

        await notificationRepository.UpdateAsync(notification);

        await hubContext.Clients.User(userId.ToString())
               .SendAsync("DeactivateNotification", notification.Id.ToString());
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var userGuid = Guid.Parse(userId);
        var unreadNotifications = await notificationRepository.FindAsync(n => n.UserId == userGuid && !n.IsRead);

        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead();
            await notificationRepository.UpdateAsync(notification);        
        }
    }
}
