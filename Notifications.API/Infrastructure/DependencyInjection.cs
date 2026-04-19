using Microsoft.EntityFrameworkCore;
using Notifications.API.Application.Interfaces;
using Notifications.API.Domain.Interfaces;
using Notifications.API.Infrastructure.Caching;
using Notifications.API.Infrastructure.Context;
using Notifications.API.Infrastructure.Repositories;
using StackExchange.Redis;

namespace Notifications.API.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
         this IServiceCollection services,
         IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var config = ConfigurationOptions.Parse(redisConnectionString, true);
            return ConnectionMultiplexer.Connect(config);
        });

        services.AddSingleton<ICacheService, CacheService>();

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IPriceAlertRepository, PriceAlertRepository>();

        return services;
    }
}