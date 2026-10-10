using Market.API.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Market.API.Controllers;

[Route("api/market-news")]
[ApiController]
public class MarketNewsController(IMarketNewsService marketNewsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRecentNews([FromQuery] int count = 10)
    {
        if (count <= 0 || count > 100)
        {
            return BadRequest("Count must be between 1 and 100.");
        }

        var news = await marketNewsService.GetRecentNewsAsync(count);

        if(news == null)
        {
            return NotFound($"News with count '{count}' not found.");
        }

        return Ok(news);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetNewsById(Guid id)
    {     
        var news = await marketNewsService.GetNewsByIdAsync(id);
        
        if (news == null)
        {
            return NotFound($"News with ID '{id}' not found.");
        }

        return Ok(news);
    }

    [HttpGet("coin/{symbol}")]
    public async Task<IActionResult> GetNewsByCoin(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return BadRequest("Coin symbol cannot be empty.");
        }

        var news = await marketNewsService.GetNewsByCoinSymbolAsync(symbol);

        if(news == null)
        {
            return NotFound($"News with symbol '{symbol}' not found.");
        }

        return Ok(news);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNews(Guid id)
    {  
        var result = await marketNewsService.DeleteNewsAsync(id);
        if (!result)
        {
            return BadRequest("Failed to delete the news.");
        }
        
        return Ok(new { message = "News deleted successfully." });
    }
}
