using Market.API.Application.Interfaces;
using Market.API.Domain.Interfaces;
using Market.API.Infrastructure.BackgroundServices;
using Market.API.Infrastructure.Caching;
using Market.API.Infrastructure.Context;
using Market.API.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Market.API.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        var redisConnectionString = configuration.GetValue<string>("Redis:ConnectionString");

        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnectionString!));

        services.AddSingleton<IRedisCacheService, RedisCacheService>();

        services.AddScoped<ICoinRepository, CoinRepository>();
        services.AddScoped<IMarketNewsRepository, MarketNewsRepository>();
        services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();

        services.AddHostedService<DatabasePriceUpdate>();
        services.AddHostedService<PriceSimulation>();
        services.AddHostedService<PriceHistoryGenerator>();
        services.AddHostedService<SupplySimulation>();
        services.AddHostedService<DatabaseSupplyUpdate>();
        services.AddHostedService<MarketNewsGenerator>();

        return services;
    }
}
