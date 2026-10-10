using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.API.Application.DTOs;
using Notifications.API.Application.Interfaces;
using Notifications.API.Models;
using System.Security.Claims;

namespace Notifications.API.Controllers
{
    [Route("api/notifications")]
    [Authorize]
    [ApiController]
    public class NotificationController(INotificationService notificationService) : ControllerBase
    {

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetUserNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid or broken token.");
            }
            var notifications = await notificationService.GetUserNotificationsAsync(userId);
            return Ok(notifications);
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid or broken token.");
            }

            var count = await notificationService.GetUnreadCountAsync(userId);
            return Ok(count);
        }

        [HttpPut("{notificationId}/read")]
        public async Task<IActionResult> MarkAsRead(Guid notificationId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Invalid or broken token.");
                }

                await notificationService.MarkAsReadAsync(notificationId, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { ex.Message });
            }
        }

        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid or broken token.");
            }

            await notificationService.MarkAllAsReadAsync(userId);
            return NoContent();
        }

        [HttpPost]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Invalid or broken token.");
            }

            await notificationService.CreateNotificationAsync(
                userId,
                request.Title,
                request.Message,
                request.Type,
                request.RelatedEntityId);

            return Ok(new { Message = "Notification created succesfully." });
        }
    }
}
