using Microsoft.EntityFrameworkCore;
using Notifications.API.Application.Interfaces;
using Notifications.API.Application.Services;
using Notifications.API.Application.Settings;
using Notifications.API.Infrastructure.Context;

namespace Notifications.API.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
         this IServiceCollection services,
         IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
       services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPriceAlertService, PriceAlertService>();

        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        services.AddTransient<IEmailService, EmailService>();

        return services;
    }
}