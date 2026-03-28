using Microsoft.EntityFrameworkCore;
using Notifications.API.Application.Interfaces;
using Notifications.API.Application.Services;
using Notifications.API.Infrastructure.Context;

namespace Notifications.API.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
         this IServiceCollection services,
         IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default Connection");
       services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        //services.AddAutoMapper(cfg => cfg.AddProfile<UserMapping>());

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPriceAlertService, PriceAlertService>();

        return services;
    }
}