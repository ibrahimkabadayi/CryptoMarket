using Market.API.Application.DTOs;

namespace Market.API.Application.Interfaces;

public interface IMarketNewsService
{
    Task<MarketNewsDto> CreateMarketNewsAsync(string title, string content, List<string> relatedSymbols);
    Task<List<MarketNewsDto>> GetRecentNewsAsync(int count = 10);
    Task<MarketNewsDto?> GetNewsByIdAsync(string id);
    Task<List<MarketNewsDto>> GetNewsByCoinSymbolAsync(string symbol);
    Task<bool> DeleteNewsAsync(string id);
}
