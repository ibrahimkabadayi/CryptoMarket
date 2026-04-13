using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Notifications.API.Application.DTOs;
using Notifications.API.Application.Interfaces;
using Notifications.API.Models;

namespace Notifications.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PriceAlertController(IPriceAlertService priceAlertService) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateAlert([FromBody] CreatePriceAlertRequest request)
        {
            try
            {
                await priceAlertService.CreateAlertAsync(
                    request.UserId,
                    request.Symbol,
                    request.TargetPrice,
                    request.IsAbove);

                return Ok(new { Message = $"{request.Symbol} için fiyat alarmı başarıyla kuruldu." });
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

        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> DeactivateAlert(Guid id, [FromBody] Guid userId)
        {
            try
            {
                await priceAlertService.DeactivateAlertAsync(id, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
