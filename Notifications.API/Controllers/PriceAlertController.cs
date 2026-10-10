using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.API.Application.DTOs;
using Notifications.API.Application.Interfaces;
using Notifications.API.Models;
using System.Security.Claims;

namespace Notifications.API.Controllers
{
    [Route("api/price-alerts")]
    [Authorize]
    [ApiController]
    public class PriceAlertController(IPriceAlertService priceAlertService) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateAlert([FromBody] CreatePriceAlertRequest request)
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return BadRequest();
            }

            var alert = await priceAlertService.CreateAlertAsync(
                userId,
                request.Symbol,
                request.TargetPrice,
                request.IsAbove);

            return Ok(new { Message = $"{request.Symbol} price alert created successfully." });
            
        }

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<PriceAlertDto>>> GetActiveAlerts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return BadRequest();
            }

            var alerts = await priceAlertService.GetActiveAlertsByUserAsync(userId);
            return Ok(alerts);
        }

        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<PriceAlertDto>>> GetAllAlerts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return BadRequest();
            }

            var alerts = await priceAlertService.GetAllAlertsByUserAsync(userId);
            return Ok(alerts);
        }

        [HttpPut("{priceAlertId}/deactivate")]
        public async Task<IActionResult> DeactivateAlert(Guid priceAlertId)
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return BadRequest();
            }

            await priceAlertService.DeactivateAlertAsync(priceAlertId, userId);
            return NoContent();
        }
    }
}
