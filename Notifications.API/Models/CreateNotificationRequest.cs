using Notifications.API.Domain.Enums;

namespace Notifications.API.Models;

public class CreateNotificationRequest
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string? RelatedEntityId { get; set; }

}
