using Bybit.Net.Interfaces.Clients;
using CryptoExchange.Net.Interfaces.Clients;
using MassTransit;
using Shared.Messages;

namespace Market.API.Infrastructure.BackgroundServices;

public class BybitPriceWorker(
    IBybitSocketClient socketClient,
    IServiceScopeFactory scopeFactory,
    ILogger<BybitPriceWorker> logger) : BackgroundService
{
    
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Bybit WebSocket bağlantısı başlatılıyor...");

        using var scope = scopeFactory.CreateScope();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var symbolsToListen = new[] { "BTCUSDT", "SOLUSDT", "LINKUSDT" };

        var subscribeResult = await socketClient.V5SpotApi.SubscribeToTickerUpdatesAsync(
            symbolsToListen,
            async data =>
            {
                var symbol = data.Data.Symbol.Replace("USDT", "");
                var currentPrice = data.Data.LastPrice;
                var safePrice = Math.Round(currentPrice, 4);

                await publishEndpoint.Publish(new CoinPriceEvent
                {
                    Symbol = symbol,
                    Price = safePrice
                }, stoppingToken);

                logger.LogInformation($"[BYBIT] {symbol} Fiyatı: {safePrice}");
            });

        if (!subscribeResult.Success)
        {
            logger.LogError($"Bybit bağlantısı koptu veya kurulamadı: {subscribeResult.Error}");
        }
        else
        {
            logger.LogInformation("Bybit WebSocket bağlantısı BAŞARILI! Canlı veriler akıyor...");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
    
}
