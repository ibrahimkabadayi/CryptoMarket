using Market.API.Application.Interfaces;
using Market.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Market.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MarketController(ICoinService coinService) : ControllerBase
{
    [HttpPost("add_coin")]
    public async Task<IActionResult> AddCoin([FromBody] AddCoinRequest request)
    {
        var result = await coinService.AddCoin(request.Name, request.Symbol, request.Price, request.MarketCap);

        if (result.StartsWith("Success"))
            return Ok();
        else
            return BadRequest(result);
    }

    [HttpGet("get_all_coins")]
    public async Task<IActionResult> GetAllCoins()
    {
        var allCoins = await coinService.GetAllCoins();

        return Ok(allCoins);
    }

    [HttpGet("getcoin/{symbol}")]
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
