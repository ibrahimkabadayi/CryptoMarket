
using Market.API.Domain.Interfaces;

namespace Market.API.Infrastructure.BackgroundServices;

public class PriceSimulationBackgroundService(IServiceScopeFactory scopeFactory,
    ILogger<PriceSimulationBackgroundService> logger) : BackgroundService
{
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Piyasa Simülasyon Motoru çalışmaya başladı...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var coinRepository = scope.ServiceProvider.GetRequiredService<ICoinRepository>();

                var coins = await coinRepository.GetAllAsync();
                var random = new Random();

                foreach (var coin in coins)
                {
                    double fluctuationPercentage = (random.NextDouble() * 2) - 1;
                    double priceChange = coin.CurrentPrice * (fluctuationPercentage / 100);

                    coin.CurrentPrice += priceChange;
                    coin.LastUpdated = DateTime.UtcNow;

                    await coinRepository.UpdateAsync(coin.Id, coin);

                    logger.LogInformation($"{coin.Symbol} yeni fiyatı: {coin.CurrentPrice:C2}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Simülasyon sırasında hata: {ex.Message}");
            }

            await Task.Delay(5000, stoppingToken);
        }
    }
}
