using Market.API.Domain.Interfaces;
using Market.API.Infrastructure.BackgroundServices;
using Market.API.Infrastructure.Context;
using Market.API.Infrastructure.Repositories;
using MongoDB.Driver;

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

        services.AddScoped<ICoinRepository, CoinRepository>();
        services.AddScoped<IMarketNewsRepository, MarketNewsRepository>();
        services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();

        services.AddHostedService<PriceSimulationBackgroundService>();

        services.AddSingleton<MarketDbContext>();

        return services;
    }
}
