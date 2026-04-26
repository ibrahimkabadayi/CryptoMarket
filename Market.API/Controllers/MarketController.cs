using Market.API.Application.Interfaces;
using Market.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Market.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MarketController(ICoinService coinService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> AddCoin([FromBody] AddCoinRequest request)
    {
        await coinService.AddCoin(request.Name, request.Symbol, request.Price, request.MarketCap);

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
}
