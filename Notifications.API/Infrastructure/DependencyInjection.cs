using Market.API.Infrastructure.Context;
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

        services.AddScoped<IMarketContext, MarketContext>();

        return services;
    }
}
