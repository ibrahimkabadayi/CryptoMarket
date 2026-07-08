using AutoMapper;
using Market.API.Application.DTOs;
using Market.API.Application.Interfaces;
using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;
using Market.API.Infrastructure.BackgroundServices.Helpers;

namespace Market.API.Infrastructure.BackgroundServices
{
    public class SupplySimulation(IServiceScopeFactory scopeFactory, IRedisCacheService cacheService, IMapper mapper, ILogger<SupplySimulation> logger) : BackgroundService
    {
        private Dictionary<string, TrendState> _trends = [];
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Supply simulation has started...");

            using var scope = scopeFactory.CreateScope();
            var coinRepository = scope.ServiceProvider.GetRequiredService<ICoinRepository>();

            var cacheKey = "coins:supply";
            var coinSupplies = await cacheService.GetAsync<List<CoinSupplyDto>>(cacheKey);

            if (coinSupplies == null) 
            {
                var coins = await coinRepository.GetAllAsync();
                coinSupplies = [];
                foreach (var coin in coins)
                {
                    if (coin.IsCapped)
                        continue;

                    var supply = mapper.Map<CoinSupplyDto>(coin);
                    coinSupplies.Add(supply);
                }
            }

            var rng = new Random();
            foreach (var coinSupply in coinSupplies)
                _trends[coinSupply.Symbol] = new TrendState(rng);

            var tickCount = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {

                    foreach (var coinSupply in coinSupplies)
                    {
                        var trend = _trends[coinSupply.Symbol];
                        coinSupply.Supply = trend.NextPrice(coinSupply.Supply, rng);
                        logger.LogInformation($"Updated supply {coinSupply.Supply} ");
                    }

                    if (++tickCount % 10 == 0)
                        await cacheService.SetAsync(cacheKey, coinSupplies);
                }
                catch (Exception ex) 
                {
                    logger.LogError("Error during simulation: {Message}", ex.Message);
                }
                

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
