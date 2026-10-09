using Market.API.Application.DTOs;
using Market.API.Application.Interfaces;
using Market.API.Domain.Interfaces;

namespace Market.API.Infrastructure.BackgroundServices;

public class DatabaseSupplyUpdate(
    IServiceScopeFactory scopeFactory,
    IRedisCacheService cacheService,
    ILogger<DatabaseSupplyUpdate> logger) : BackgroundService
{
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Supply Update service has started.");

        using var scope = scopeFactory.CreateScope();
        var coinRepository = scope.ServiceProvider.GetRequiredService<ICoinRepository>();

        var cacheKey = "coins:supply";

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var coins = await cacheService.GetAsync<List<CoinSupplyDto>>(cacheKey);
                if (coins == null)
                {
                    continue;
                }

                foreach (var coin in coins)
                {
                    logger.LogInformation($"{coin.Symbol} supply update: {coin.Supply}");
                    await coinRepository.UpdateCoinSupply(coin.Symbol, coin.Supply);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Error: " + ex.Message);
            }

            await Task.Delay(100000, stoppingToken);
        }
    }
}
