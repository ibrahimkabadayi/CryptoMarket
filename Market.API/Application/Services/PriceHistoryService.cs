using Market.API.Application.DTOs;
using Market.API.Application.Interfaces;
using Market.API.Domain.Interfaces;

namespace Market.API.Application.Services;

public class PriceHistoryService(IPriceHistoryRepository priceHistoryRepository) : IPriceHistoryService
{
    public async Task<List<PriceHistoryDto>> GetPriceHistoryAsync(string symbol, int intervalMinutes, DateTime startDate, DateTime endDate)
    {
        var allCandles = await priceHistoryRepository.FindAsync(x =>
            x.Symbol == symbol &&
            x.Timestamp >= startDate &&
            x.Timestamp <= endDate);

        if (allCandles == null || !allCandles.Any())
            return new List<PriceHistoryDto>();

        var aggregatedCandles = allCandles
            .OrderBy(x => x.Timestamp)
            .GroupBy(x =>
            {
                var timeSpan = x.Timestamp - DateTime.UnixEpoch;
                var totalMinutes = (long)timeSpan.TotalMinutes;
                var periodStartMinute = (totalMinutes / intervalMinutes) * intervalMinutes;
                return DateTime.UnixEpoch.AddMinutes(periodStartMinute);
            })
            .Select(g => new PriceHistoryDto(
                Symbol: symbol,
                OpenPrice: g.First().OpenPrice,         
                ClosePrice: g.Last().ClosePrice,
                HighPrice: g.Max(c => c.HighPrice),
                LowPrice: g.Min(c => c.LowPrice),
                Volume: g.Sum(c => c.Volume),
                Timestamp: g.Key
            ))
            .ToList();

        return aggregatedCandles;
    }
}
