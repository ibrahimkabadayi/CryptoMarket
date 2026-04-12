using Notifications.API.Domain.Enums;

namespace Notifications.API.Application.DTOs;

public class NotificationDto : BaseDto
{
    public Guid UserId { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;

    public NotificationType Type { get; private set; }

    public bool IsRead { get; private set; }

    public DateTime? ReadAt { get; private set; }

    public string? RelatedEntityId { get; private set; }

    public NotificationDto(Guid userId, string title, string message, NotificationType type, string? relatedEntityId = null)
    {
        if (userId == Guid.Empty) throw new ArgumentException("Kullanıcı ID boş olamaz.", nameof(userId));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Başlık boş olamaz.", nameof(title));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Mesaj boş olamaz.", nameof(message));

        UserId = userId;
        Title = title;
        Message = message;
        Type = type;
        IsRead = false;
        RelatedEntityId = relatedEntityId;
    }

    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
    }
}
