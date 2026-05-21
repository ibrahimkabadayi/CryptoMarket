using System.Security.Claims;
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
    [HttpPost]
    public async Task<IActionResult> AddCoin([FromBody] AddCoinRequest request)
    {
        await coinService.AddCoin(request.Name, request.Symbol, request.Price, request.MarketCap);

        return Ok();
    }

    [Authorize]
    [HttpPost("{symbol}")]
    public IActionResult BuyCoin(string symbol, BuyCoinRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) 
        { 
            BadRequest(); 
        }

        var buyCoinDto = new BuyCoinDto 
        {
            UserId = Guid.Parse(userId!),
            BuyAmount = request.Amount,
            BuyPrice = request.Price,
            Symbol = symbol
        };

        coinService.BuyCoin(buyCoinDto);

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
        return Ok(coin);
    }

    [HttpPatch("{symbol}")]
    public async Task<IActionResult> UpdateCoin(string symbol, UpdateCoinRequest request) 
    {
        await coinService.UpdateCoin(symbol, request.Price, request.MarketCap);
        return Ok();
    }

    [HttpGet("{symbol}/history")]
    public async Task<IActionResult> GetCoinHistory(
        string symbol,
        [FromQuery] int intervalMinutes = 15,  
        [FromQuery] int hoursBack = 24)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddHours(-hoursBack);

        var history = await priceHistoryService.GetPriceHistoryAsync(symbol, intervalMinutes, startDate, endDate);

        return Ok(history);
    }
}
