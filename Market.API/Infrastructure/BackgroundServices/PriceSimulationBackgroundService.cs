using Market.API.Application.Interfaces;
using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;
using MassTransit;
using Shared.Messages;

namespace Market.API.Infrastructure.BackgroundServices;

public class PriceSimulationBackgroundService(IServiceScopeFactory scopeFactory,
    ILogger<PriceSimulationBackgroundService> logger, IRedisCacheService cacheService) : BackgroundService
{
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Simulation has started...");

        using var scope = scopeFactory.CreateScope();
        var coinRepository = scope.ServiceProvider.GetRequiredService<ICoinRepository>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var cacheKey = "market:coins";
        var coins = await cacheService.GetAsync<List<Coin>>(cacheKey);

        if (coins == null || coins.Any())
        {
            coins = await coinRepository.GetAllAsync();
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var random = new Random();

                foreach (var coin in coins)
                {
                    decimal fluctuationPercentage = ((decimal)random.NextDouble() * 2) - 1;
                    decimal priceChange = coin.CurrentPrice * (fluctuationPercentage / 100);

                    coin.CurrentPrice += priceChange;
                    coin.LastUpdated = DateTime.UtcNow;

                    //logger.LogInformation($"{coin.Symbol} yeni fiyatı: {coin.CurrentPrice:C2}");

                    await publishEndpoint.Publish(new CoinPriceEvent { Price = coin.CurrentPrice, Symbol = coin.Symbol}, stoppingToken);
                }

                await cacheService.SetAsync(cacheKey, coins);
            }
            catch (Exception ex)
            {
                logger.LogError($"Error during simulation: {ex.Message}");
            }

            await Task.Delay(100, stoppingToken);
        }
    }
}
