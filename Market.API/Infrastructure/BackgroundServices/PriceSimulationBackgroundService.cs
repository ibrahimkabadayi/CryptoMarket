using Market.API.Application.Interfaces;
using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;
using MassTransit;
using Shared.Messages;

namespace Market.API.Infrastructure.BackgroundServices;

public class PriceSimulationBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<PriceSimulationBackgroundService> logger,
    IRedisCacheService cacheService) : BackgroundService
{
    private readonly Dictionary<string, TrendState> _trends = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Simulation has started...");

        using var scope = scopeFactory.CreateScope();
        var coinRepository = scope.ServiceProvider.GetRequiredService<ICoinRepository>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var cacheKey = "market:coins";
        var coins = await cacheService.GetAsync<List<Coin>>(cacheKey);
        if (coins == null || !coins.Any())
            coins = await coinRepository.GetAllAsync();

        var rng = new Random();
        foreach (var coin in coins)
            _trends[coin.Symbol] = new TrendState(rng);

        var tickCount = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var coin in coins)
                {
                    var trend = _trends[coin.Symbol];
                    coin.CurrentPrice = trend.NextPrice(coin.CurrentPrice, rng);
                    coin.LastUpdated = DateTime.UtcNow;

                    logger.LogInformation($"{coin.Symbol} Price: {coin.CurrentPrice}");

                    await publishEndpoint.Publish(
                        new CoinPriceEvent { Price = coin.CurrentPrice, Symbol = coin.Symbol },
                        stoppingToken);
                }

                if (++tickCount % 100 == 0)
                    await cacheService.SetAsync(cacheKey, coins);
            }
            catch (Exception ex)
            {
                logger.LogError("Error during simulation: {Message}", ex.Message);
            }

            await Task.Delay(100, stoppingToken);
        }
    }
}

internal class TrendState
{
    private decimal _momentum = 0;
    private int _remainingTicks = 0;

    private const decimal BaseVolatility = 0.0008m;
    private const decimal MomentumFactor = 0.6m;

    public TrendState(Random rng)
    {
        ResetTrend(rng);
    }

    public decimal NextPrice(decimal currentPrice, Random rng)
    {
        if (--_remainingTicks <= 0)
            ResetTrend(rng);

        var noise = (decimal)(rng.NextDouble() * 2 - 1) * BaseVolatility;

        var momentumEffect = _momentum * MomentumFactor;

        var spike = 0m;
        if (rng.NextDouble() < 0.002)
            spike = (decimal)(rng.NextDouble() * 2 - 1) * BaseVolatility * 5;

        var totalChange = noise + momentumEffect + spike;

        var newPrice = Math.Max(currentPrice * (1 + totalChange), 0.0001m);

        return Math.Round(newPrice, 2);
    }

    private void ResetTrend(Random rng)
    {
        var direction = rng.NextDouble();
        _momentum = direction switch
        {
            < 0.35 => (decimal)(rng.NextDouble() * 0.0003),
            < 0.70 => -(decimal)(rng.NextDouble() * 0.0003),
            _ => (decimal)(rng.NextDouble() * 0.0001 - 0.00005)
        };

        _remainingTicks = rng.Next(20, 200);
    }
}
