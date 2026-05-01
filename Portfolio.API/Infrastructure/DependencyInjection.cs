using Microsoft.EntityFrameworkCore;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Domain.Interfaces;
using Portfolio.API.Infrastructure.Caching;
using Portfolio.API.Infrastructure.Context;
using Portfolio.API.Infrastructure.Repositories;
using StackExchange.Redis;

namespace Portfolio.API.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
         this IServiceCollection services,
         IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        var redisConnectionString = configuration.GetValue<string>("Redis:ConnectionString");

        services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnectionString!));

        services.AddSingleton<ICacheService, CacheService>();

        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<ITreasuryBalanceRepository, TreasuryBalanceRepository>();
        services.AddScoped<ILimitOrderRepository, LimitOrderRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();

        return services;
    }
}
