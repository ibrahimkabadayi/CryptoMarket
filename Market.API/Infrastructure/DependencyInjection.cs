using Market.API.Application.Interfaces;
using Market.API.Domain.Interfaces;
using Market.API.Infrastructure.BackgroundServices;
using Market.API.Infrastructure.Caching;
using Market.API.Infrastructure.Context;
using Market.API.Infrastructure.Repositories;
using MongoDB.Driver;
using StackExchange.Redis;

namespace Market.API.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IMongoClient>(sp =>
        {
            var connectionString = configuration.GetValue<string>("DatabaseSettings:ConnectionString");
            return new MongoClient(connectionString);
        });

        services.Configure<MongoDbSettings>(configuration.GetSection("MongoDbSettings"));

        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect("redis_db:6379,abortConnect=false"));

        services.AddSingleton<IRedisCacheService, RedisCacheService>();

        services.AddScoped<ICoinRepository, CoinRepository>();
        services.AddScoped<IMarketNewsRepository, MarketNewsRepository>();
        services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();

        services.AddHostedService<PriceSimulationBackgroundService>();
        services.AddHostedService<DatabasePriceUpdateBackgroundService>();

        services.AddSingleton<MarketDbContext>();

        return services;
    }
}
