using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Market.API.Application.DTOs;
using Market.API.Application.Interfaces;
using Market.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Market.API.Controllers;

[Route("api/market")]
[ApiController]
public class MarketController(ICoinService coinService, IPriceHistoryService priceHistoryService, ILimitOrderService limitOrderService) : ControllerBase
{
    [Authorize]
    [HttpPost("{symbol}")]
    public async Task<IActionResult> BuyCoin(string symbol, BuyCoinRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) 
        { 
            return BadRequest(); 
        }

        var buyCoinDto = new BuyCoinDto 
        {
            UserId = Guid.Parse(userId!),
            BuyAmount = request.Amount,
            BuyPrice = request.Price,
            Symbol = symbol
        };

        await coinService.BuyCoin(buyCoinDto);

        return Ok();
    }

    [Authorize]
    [HttpPost("limit-order/{symbol}")]
    public async Task<IActionResult> SetLimitOrder(string symbol, SetLimitOrderRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) 
        { 
            return BadRequest(); 
        }

        await limitOrderService.SetLimitOrder(symbol, Guid.Parse(userId), request.TargetPrice, request.Amount, request.OrderType);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCoins()
    {
        var allCoins = await coinService.GetAllCoins();
        return Ok(allCoins);
    }

    [HttpGet("{symbol}")]
    public async Task<IActionResult> GetCoin(string symbol)
    {
        var coin = await coinService.GetCoinBySymbol(symbol);

        if(coin == null)
        {
            return NotFound($"Coin with symbol {symbol} not found.");
        }

        return Ok(coin);
    }

    [HttpGet("{symbol}/history")]
    public async Task<IActionResult> GetCoinHistory(
        string symbol,
        [Range(5, 1440)] [FromQuery] int intervalMinutes = 15,  
        [Range(1, 720)] [FromQuery] int hoursBack = 24)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddHours(-hoursBack);

        var history = await priceHistoryService.GetPriceHistoryAsync(symbol, intervalMinutes, startDate, endDate);

        return Ok(history);
    }
}
