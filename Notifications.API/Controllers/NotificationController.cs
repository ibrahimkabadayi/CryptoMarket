using Microsoft.AspNetCore.Mvc;
using Notifications.API.Application.DTOs;
using Notifications.API.Application.Interfaces;

namespace Notifications.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController(INotificationService notificationService) : ControllerBase
    {

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUserNotifications(Guid userId)
        {
            var notifications = await notificationService.GetUserNotificationsAsync(userId);
            return Ok(notifications);
        }

        [HttpGet("user/{userId}/unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount(Guid userId)
        {
            var count = await notificationService.GetUnreadCountAsync(userId);
            return Ok(count);
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id, [FromBody] Guid userId)
        {
            try
            {
                await notificationService.MarkAsReadAsync(id, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { ex.Message });
            }
        }

        [HttpPut("user/{userId}/mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead(Guid userId)
        {
            await notificationService.MarkAllAsReadAsync(userId);
            return NoContent();
        }

        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest request)
        {
            await notificationService.CreateNotificationAsync(
                request.UserId,
                request.Title,
                request.Message,
                request.Type,
                request.RelatedEntityId);

            return Ok(new { Message = "Bildirim başarıyla oluşturuldu." });
        }
    }
}
