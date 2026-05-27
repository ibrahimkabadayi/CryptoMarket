using Market.API.Application.Interfaces;
using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;
using Market.API.Hubs;
using Market.API.Hubs.Messages;
using Microsoft.AspNetCore.SignalR;

namespace Market.API.Infrastructure.BackgroundServices;

public class PriceHistoryBackgroundService(
    IServiceScopeFactory scopeFactory,
    IRedisCacheService cacheService,
    IHubContext<MarketHub> hubContext,
    ILogger<PriceHistoryBackgroundService> logger) : BackgroundService
{
    private readonly Dictionary<string, decimal> _lastClosePrices = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OHLCV Price History Snapshot Service has started...");

        var cacheKey = "market:coins";
        var rng = new Random();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var coins = await cacheService.GetAsync<List<Coin>>(cacheKey);

                if (coins != null && coins.Any())
                {
                    using var scope = scopeFactory.CreateScope();
                    var priceHistoryRepository = scope.ServiceProvider.GetRequiredService<IPriceHistoryRepository>();

                    foreach (var coin in coins)
                    {
                        var currentPrice = coin.CurrentPrice;

                        if (!_lastClosePrices.TryGetValue(coin.Symbol, out var openPrice))
                        {
                            openPrice = currentPrice;
                        }

                        var closePrice = currentPrice;

                        var maxPrice = Math.Max(openPrice, closePrice);
                        var highPrice = maxPrice + (maxPrice * (decimal)rng.NextDouble() * 0.002m);

                        var minPrice = Math.Min(openPrice, closePrice);
                        var lowPrice = minPrice - (minPrice * (decimal)rng.NextDouble() * 0.002m);

                        var volume = (decimal)rng.Next(50, 5000);

                        var history = new PriceHistory
                        {
                            Symbol = coin.Symbol,
                            OpenPrice = Math.Round(openPrice, 2),
                            ClosePrice = Math.Round(closePrice, 2),
                            HighPrice = Math.Round(highPrice, 2),
                            LowPrice = Math.Round(lowPrice, 2),
                            Volume = Math.Round(volume, 2),
                            Timestamp = DateTime.UtcNow
                        };

                        await priceHistoryRepository.AddAsync(history);

                        _lastClosePrices[coin.Symbol] = closePrice;

                        await hubContext.Clients.All.SendAsync(
                            "ReceiveHistoryUpdate",
                            history,
                            stoppingToken);
                    }

                    logger.LogInformation("Successfully saved OHLCV candlestick snapshot for {Count} coins.", coins.Count);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while saving price history.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}