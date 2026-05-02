using System.Net;
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
        //var proxyHost = configuration["ProxySettings:Host"];
        //var proxyPortStr = configuration["ProxySettings:Port"];

        //if (!string.IsNullOrEmpty(proxyHost) && int.TryParse(proxyPortStr, out int proxyPort))
        //{
        //    var webProxy = new WebProxy(proxyHost, proxyPort);

        //    var proxyUsername = configuration["ProxySettings:Username"];
        //    var proxyPassword = configuration["ProxySettings:Password"];

        //    if (!string.IsNullOrEmpty(proxyUsername) && !string.IsNullOrEmpty(proxyPassword))
        //    {
        //        webProxy.Credentials = new NetworkCredential(proxyUsername, proxyPassword);
        //    }

        //    WebRequest.DefaultWebProxy = webProxy;
        //    HttpClient.DefaultProxy = webProxy;
        //}


        services.AddSingleton<IMongoClient>(sp =>
        {
            var connectionString = configuration.GetValue<string>("DatabaseSettings:ConnectionString");
            return new MongoClient(connectionString);
        });

        services.Configure<MongoDbSettings>(configuration.GetSection("MongoDbSettings"));

        var redisConnectionString = configuration.GetValue<string>("Redis:ConnectionString");

        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnectionString!));

        services.AddSingleton<IRedisCacheService, RedisCacheService>();

        services.AddScoped<ICoinRepository, CoinRepository>();
        services.AddScoped<IMarketNewsRepository, MarketNewsRepository>();
        services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();

        services.AddHostedService<DatabasePriceUpdateBackgroundService>();
        services.AddHostedService<PriceSimulationBackgroundService>();

        //services.AddHostedService<PriceSimulatorWorker>();
        //services.AddHostedService<BybitPriceWorker>();

        //services.AddHttpClient("bybit", client =>
        //{
        //    client.BaseAddress = new Uri("https://api.bybit.com/");
        //    client.DefaultRequestHeaders.Add("Accept", "application/json");
        //});

        services.AddSingleton<MarketDbContext>();

        return services;
    }
}
