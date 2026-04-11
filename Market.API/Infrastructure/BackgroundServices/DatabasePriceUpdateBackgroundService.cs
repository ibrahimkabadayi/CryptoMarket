
using Market.API.Application.Interfaces;
using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;

namespace Market.API.Infrastructure.BackgroundServices;

public class DatabasePriceUpdateBackgroundService(IServiceScopeFactory scopeFactory, IRedisCacheService cacheService, ILogger<DatabasePriceUpdateBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Continuous database update machine for coins started");

        using var scope = scopeFactory.CreateScope();
        var coinRepository = scope.ServiceProvider.GetRequiredService<ICoinRepository>();

        var cacheKey = "market:coins";

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var coins = await cacheService.GetAsync<List<Coin>>(cacheKey);
                if (coins == null)
                {
                    continue;
                }

                foreach (Coin coin in coins)
                {
                    await coinRepository.UpdateAsync(coin.Id, coin);
                }
            }
            catch(Exception ex)
            {
                logger.LogError("Error: " + ex.Message);
            }          

            await Task.Delay(1000, stoppingToken);
        }
    }
}
