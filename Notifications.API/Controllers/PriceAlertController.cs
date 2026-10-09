using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Notifications.API.Application.DTOs;
using Notifications.API.Application.Interfaces;
using Notifications.API.Hubs;
using Notifications.API.Models;

namespace Notifications.API.Controllers
{
    [Route("api/price-alerts")]
    [ApiController]
    public class PriceAlertController(IPriceAlertService priceAlertService, IHubContext<NotificationHub> hubContext) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateAlert([FromBody] CreatePriceAlertRequest request)
        {
            try
            {
                var alert = await priceAlertService.CreateAlertAsync(
                    request.UserId,
                    request.Symbol,
                    request.TargetPrice,
                    request.IsAbove);

                await hubContext.Clients.User(request.UserId.ToString())
                    .SendAsync("ReceivePriceAlert", alert);

                return Ok(new { Message = $"{request.Symbol} price alert created successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { ex.Message });
            }
        }

        [HttpGet("user/{userId}/active")]
        public async Task<ActionResult<IEnumerable<PriceAlertDto>>> GetActiveAlerts(Guid userId)
        {
            var alerts = await priceAlertService.GetActiveAlertsByUserAsync(userId);
            return Ok(alerts);
        }

        [HttpGet("user/{userId}/all")]
        public async Task<ActionResult<IEnumerable<PriceAlertDto>>> GetAllAlerts(Guid userId)
        {
            var alerts = await priceAlertService.GetAllAlertsByUserAsync(userId);
            return Ok(alerts);
        }

        [HttpPut("{priceAlertId}/deactivate")]
        public async Task<IActionResult> DeactivateAlert(Guid priceAlertId, [FromBody] Guid userId)
        {
            try
            {
                await priceAlertService.DeactivateAlertAsync(priceAlertId, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { ex.Message });
            }
        }
    }
}
