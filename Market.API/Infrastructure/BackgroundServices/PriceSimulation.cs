using Market.API.Application.Interfaces;
using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;
using Market.API.Hubs;
using Market.API.Hubs.Messages;
using Market.API.Infrastructure.BackgroundServices.Helpers;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Shared.Messages;

namespace Market.API.Infrastructure.BackgroundServices;

public class PriceSimulation(
    IServiceScopeFactory scopeFactory,
    ILogger<PriceSimulation> logger,
    IRedisCacheService cacheService,
    IHubContext<MarketHub> hubContext) : BackgroundService
{
    private readonly Dictionary<string, TrendState> _trends = [];
    private readonly Dictionary<string, decimal> _pastPrices = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Simulation has started...");

        using var scope = scopeFactory.CreateScope();
        var coinRepository = scope.ServiceProvider.GetRequiredService<ICoinRepository>();
        var priceHistoryRepository = scope.ServiceProvider.GetRequiredService<IPriceHistoryRepository>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var cacheKey = "market:coins";
        var coins = await cacheService.GetAsync<List<Coin>>(cacheKey);
        if (coins == null || coins.Count == 0)
            coins = await coinRepository.GetAllAsync();

        var rng = new Random();

        foreach (var coin in coins)
        {
            _trends[coin.Symbol] = new TrendState(rng);
            var priceHistory = await priceHistoryRepository.GetPriceHistory(coin.Symbol, TimeSpan.FromDays(1));
            _pastPrices[coin.Symbol] = (priceHistory.ClosePrice + priceHistory.OpenPrice) / 2;
        }

        var tickCount = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var coin in coins)
                {
                    var trend = _trends[coin.Symbol];
                    coin.CurrentPrice = trend.NextPrice(coin.CurrentPrice, rng);
                    coin.MarketCap = coin.CurrentPrice * coin.Supply;
                    coin.LastUpdated = DateTime.UtcNow;

                    var percentChange = (coin.CurrentPrice - _pastPrices[coin.Symbol]) / _pastPrices[coin.Symbol] * 100;

                    await publishEndpoint.Publish(
                        new CoinPriceEvent { Price = coin.CurrentPrice, Symbol = coin.Symbol },
                        stoppingToken);

                    var priceUpdateMessage = new PriceUpdateMessage(coin.Symbol, coin.CurrentPrice, coin.MarketCap, percentChange);

                    await hubContext.Clients.All.SendAsync(
                        "ReceivePriceUpdate",
                        priceUpdateMessage,
                        stoppingToken);
                }

                if (++tickCount % 30 == 0) 
                {
                    foreach (var coin in coins)
                    {
                        var priceHistory = await priceHistoryRepository.GetPriceHistory(coin.Symbol, TimeSpan.FromDays(1));
                        _pastPrices[coin.Symbol] = (priceHistory.ClosePrice + priceHistory.OpenPrice) / 2;
                    }
                }

                if (++tickCount % 100 == 0) 
                    await cacheService.SetAsync(cacheKey, coins);
            }
            catch (Exception ex)
            {
                logger.LogError("Error during simulation: {Message}", ex.Message);
            }

            await Task.Delay(2000, stoppingToken);
        }
    }
}


