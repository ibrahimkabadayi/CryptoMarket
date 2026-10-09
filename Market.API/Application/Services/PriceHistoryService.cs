using Market.API.Application.DTOs;
using Market.API.Application.Interfaces;
using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;

namespace Market.API.Application.Services;

public class PriceHistoryService(IPriceHistoryRepository priceHistoryRepository, ICoinRepository coinRepository) : IPriceHistoryService
{
    public async Task<List<PriceHistoryDto>> GetPriceHistoryAsync(string symbol, int intervalMinutes, DateTime startDate, DateTime endDate)
    {
        var allCandles = await priceHistoryRepository.FindAsync(x =>
            x.Symbol == symbol &&
            x.Timestamp >= startDate &&
            x.Timestamp <= endDate);

        var candlesList = allCandles?.OrderBy(x => x.Timestamp).ToList() ?? new List<PriceHistory>();
        var coin = await coinRepository.GetCoinAsync(symbol);
        decimal currentPrice = coin?.CurrentPrice ?? 0;

        var alignedStartDate = GetAlignedTime(startDate, intervalMinutes);
        var alignedEndDate = GetAlignedTime(endDate, intervalMinutes);

        var previousCandles = await priceHistoryRepository.FindAsync(x => x.Symbol == symbol && x.Timestamp < startDate);
        var anchor = previousCandles?.OrderByDescending(x => x.Timestamp).FirstOrDefault();
        
        decimal lastKnownPrice = anchor?.ClosePrice ?? currentPrice;

        var intervals = new List<PriceHistoryDto>();
        var currentTime = alignedStartDate;
        
        var nonZeroVolumeCandles = candlesList.Where(c => c.Volume > 0).ToList();
        var averageVolume = nonZeroVolumeCandles.Count > 0 ? nonZeroVolumeCandles.Average(c => c.Volume) : 1000m;

        while (currentTime <= alignedEndDate)
        {
            var nextTime = currentTime.AddMinutes(intervalMinutes);
            var candlesInInterval = candlesList.Where(x => x.Timestamp >= currentTime && x.Timestamp < nextTime).ToList();

            if (candlesInInterval.Count > 0)
            {
                intervals.Add(new PriceHistoryDto(
                    Symbol: symbol,
                    OpenPrice: candlesInInterval.First().OpenPrice,
                    ClosePrice: candlesInInterval.Last().ClosePrice,
                    HighPrice: candlesInInterval.Max(c => c.HighPrice),
                    LowPrice: candlesInInterval.Min(c => c.LowPrice),
                    Volume: candlesInInterval.Sum(c => c.Volume),
                    Timestamp: currentTime
                ));
            }
            else
            {
                intervals.Add(new PriceHistoryDto(
                    Symbol: symbol,
                    OpenPrice: 0,
                    ClosePrice: 0,
                    HighPrice: 0,
                    LowPrice: 0,
                    Volume: -1,
                    Timestamp: currentTime
                ));
            }

            currentTime = nextTime;
        }

        for (int i = 0; i < intervals.Count; i++)
        {
            if (intervals[i].Volume == -1)
            {
                int gapStart = i;
                int gapEnd = i;
                while (gapEnd + 1 < intervals.Count && intervals[gapEnd + 1].Volume == -1)
                {
                    gapEnd++;
                }

                decimal startPrice = gapStart > 0 ? intervals[gapStart - 1].ClosePrice : lastKnownPrice;
                decimal endPrice = gapEnd + 1 < intervals.Count ? intervals[gapEnd + 1].OpenPrice : currentPrice;
                
                int gapCount = gapEnd - gapStart + 1;
                var priceRange = endPrice - startPrice;
                var volatility = Math.Abs(priceRange) * 0.08m;
                if (volatility == 0) volatility = startPrice * 0.002m;

                var seed = (intervals[gapStart].Timestamp.Ticks).GetHashCode();
                var random = new Random(seed);

                for (int j = 0; j < gapCount; j++)
                {
                    var progressRatio = (decimal)(j + 1) / (gapCount + 1);
                    var expectedPrice = startPrice + (priceRange * progressRatio);

                    var randomMovement = (decimal)(random.NextDouble() - 0.5) * volatility * 4;
                    var closePrice = expectedPrice + randomMovement;
                    
                    var openPrice = j == 0 ? startPrice : intervals[gapStart + j - 1].ClosePrice + (decimal)(random.NextDouble() - 0.5) * volatility * 3;

                    var wickRange = volatility * (decimal)(1.2 + random.NextDouble() * 1.5);
                    var highPrice = Math.Max(openPrice, closePrice) + wickRange;
                    var lowPrice = Math.Min(openPrice, closePrice) - wickRange;

                    var volumeMultiplier = 0.6m + (decimal)random.NextDouble() * 0.9m;
                    var volume = (decimal)Math.Round(averageVolume * volumeMultiplier);

                    intervals[gapStart + j] = new PriceHistoryDto(
                        Symbol: symbol,
                        OpenPrice: Math.Round(openPrice, 2),
                        ClosePrice: Math.Round(closePrice, 2),
                        HighPrice: Math.Round(highPrice, 2),
                        LowPrice: Math.Round(lowPrice, 2),
                        Volume: (long)volume,
                        Timestamp: intervals[gapStart + j].Timestamp
                    );
                }
                
                i = gapEnd;
            }
        }

        return intervals;
    }

    private static DateTime GetAlignedTime(DateTime time, int intervalMinutes)
    {
        var timeSpan = time - DateTime.UnixEpoch;
        var totalMinutes = (long)timeSpan.TotalMinutes;
        var periodStartMinute = (totalMinutes / intervalMinutes) * intervalMinutes;
        return DateTime.UnixEpoch.AddMinutes(periodStartMinute);
    }
}