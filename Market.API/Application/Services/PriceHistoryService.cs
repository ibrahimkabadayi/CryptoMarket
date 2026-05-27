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

        if (allCandles == null || allCandles.Count == 0)
            return [];

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
            .OrderBy(x => x.Timestamp)
            .ToList();

        var filledCandles = FillGapsAndZeroVolume(aggregatedCandles, intervalMinutes);

        return filledCandles;
    }

    private static List<PriceHistoryDto> FillGapsAndZeroVolume(List<PriceHistoryDto> candles, int intervalMinutes)
    {
        if (candles.Count < 2)
            return candles;

        var result = new List<PriceHistoryDto> { candles[0] };
        var averageVolume = candles.Where(c => c.Volume > 0).Average(c => c.Volume);

        for (int i = 0; i < candles.Count - 1; i++)
        {
            var current = candles[i];
            var next = candles[i + 1];
            var timeDiff = (next.Timestamp - current.Timestamp).TotalMinutes;
            var expectedIntervals = (int)(timeDiff / intervalMinutes);

            if (expectedIntervals > 1)
            {
                var syntheticCandles = GenerateSyntheticCandles(current, next, expectedIntervals - 1, intervalMinutes, averageVolume);
                result.AddRange(syntheticCandles);
            }

            result.Add(next);
        }

        return result;
    }

    private static List<PriceHistoryDto> GenerateSyntheticCandles(PriceHistoryDto from, PriceHistoryDto to, int count, int intervalMinutes, decimal averageVolume)
    {
        var synthetic = new List<PriceHistoryDto>();
        var startPrice = from.ClosePrice;
        var endPrice = to.OpenPrice;

        var priceRange = endPrice - startPrice;
        var volatility = Math.Abs(priceRange) * 0.08m;

        for (int i = 1; i <= count; i++)
        {
            var timestamp = from.Timestamp.AddMinutes(intervalMinutes * i);

            var seed = timestamp.Ticks.GetHashCode();
            var random = new Random(seed);

            var progressRatio = (decimal)i / (count + 1);
            var expectedPrice = startPrice + (priceRange * progressRatio);

            var randomMovement = (decimal)(random.NextDouble() - 0.5) * volatility * 4;
            var closePrice = expectedPrice + randomMovement;

            var openPrice = i == 1 ? startPrice : synthetic[^1].ClosePrice + (decimal)(random.NextDouble() - 0.5) * volatility * 3;

            var wickRange = volatility * (decimal)(1.2 + random.NextDouble() * 1.5); 
            var highPrice = Math.Max(openPrice, closePrice) + wickRange;
            var lowPrice = Math.Min(openPrice, closePrice) - wickRange;

            var volumeMultiplier = 0.6m + (decimal)random.NextDouble() * 0.9m;
            var volume = (decimal)Math.Round(averageVolume * volumeMultiplier);

            var syntheticCandle = new PriceHistoryDto(
                Symbol: from.Symbol,
                OpenPrice: Math.Round(openPrice, 2),
                ClosePrice: Math.Round(closePrice, 2),
                HighPrice: Math.Round(highPrice, 2),
                LowPrice: Math.Round(lowPrice, 2),
                Volume: (long)volume,
                Timestamp: timestamp
            );

            synthetic.Add(syntheticCandle);
        }

        return synthetic;
    }
}