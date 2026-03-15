using Microsoft.AspNetCore.Mvc;
using Portfolio.API.Application.DTOs;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Domain.Enums;
using Portfolio.API.Models;

namespace Portfolio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LimitOrderController(ILimitOrderService limitOrderService) : ControllerBase
    {
        [HttpPost("create_limit_order")]
        public async Task<IActionResult> CreateLimitOrder([FromBody] CreateLimitOrderRequest request)
        {
            var orderType = (request.OrderType == 1) ? LimitOrderType.Buy : LimitOrderType.Sell;

            var dto = new CreateLimitOrderDto
            {
                Amount = request.Amount,
                Symbol= request.Symbol,
                OrderType = orderType,
                TargetPrice = request.TargetPrice,
                UserId = request.UserId,
                WalletId = request.WalletId,
            };

            await limitOrderService.CreateLimitOrderAsync(dto);

            return Ok();
        }

        [HttpGet(template: "get_limit_order/{id}")]
        public async Task<IActionResult> GetLimitOrder(Guid id)
        {
            var limitOrder = await limitOrderService.GetLimitOrder(id);
            return Ok(limitOrder);
        }

        [HttpGet("get_all_limit_orders")]
        public async Task<IActionResult> GetAllLimitOrders()
        {
            var result = await limitOrderService.GetAllLimitOrders();
            return Ok(result);
        }

        [HttpPatch("update_limit_order")]
        public async Task<IActionResult> UpdateLimitOrder([FromBody] UpdateLimitOrderRequest request)
        {
            var result = await limitOrderService.UpdateLimitOrderAsync(request.Id, request.Amount, request.TargetPrice);
            return result.StartsWith("Success") ? Ok(result) : BadRequest(result);
        }

        [HttpDelete(template: "delete_limit_order/{id}")]
        public async Task<IActionResult> DeleteLimitOrder(Guid id)
        {
            await limitOrderService.DeleteLimitOrderAsync(id);
            return Ok();
        }
    }
}
