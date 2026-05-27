using AutoMapper;
using DnsClient.Internal;
using Market.API.Application.DTOs;
using Market.API.Application.Interfaces;
using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;

namespace Market.API.Application.Services;

public class MarketNewsService(IMarketNewsRepository newsRepository, IMapper mapper, ILogger<MarketNewsService> logger) : IMarketNewsService
{
    public async Task<MarketNewsDto> CreateMarketNewsAsync(string title, string content, List<string> relatedSymbols)
    {
        var news = new MarketNews
        {
            Title = title,
            Content = content,
            Source = "Gemini AI",
            RelatedSymbols = relatedSymbols,
            PublishedAt = DateTime.UtcNow
        };

        await newsRepository.AddAsync(news);
        logger.LogInformation("Created market news: {Title} for coins: {Coins}", title, string.Join(", ", relatedSymbols));

        return mapper.Map<MarketNewsDto>(news);
    }

    public async Task<MarketNewsDto?> GetNewsByIdAsync(string id)
    {
        var news = await newsRepository.GetByIdAsync(id);
        return news == null ? null : mapper.Map<MarketNewsDto>(news);
    }

    public async Task<List<MarketNewsDto>> GetRecentNewsAsync(int count = 10)
    {
        var allNews = await newsRepository.GetAllAsync();
        var recentNews = allNews
            .OrderByDescending(n => n.PublishedAt)
            .Take(count)
            .ToList();

        return mapper.Map<List<MarketNewsDto>>(recentNews);
    }
    public async Task<List<MarketNewsDto>> GetNewsByCoinSymbolAsync(string symbol)
    {
        var allNews = await newsRepository.GetAllAsync();
        var relevantNews = allNews
            .Where(n => n.RelatedSymbols.Contains(symbol, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(n => n.PublishedAt)
            .ToList();

        return mapper.Map<List<MarketNewsDto>>(relevantNews);
    }

    public async Task<bool> DeleteNewsAsync(string id)
    {
        try
        {
            await newsRepository.DeleteAsync(id);
            logger.LogInformation("Deleted market news with id: {NewsId}", id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting news with id: {NewsId}", id);
            return false;
        }
    }
}
