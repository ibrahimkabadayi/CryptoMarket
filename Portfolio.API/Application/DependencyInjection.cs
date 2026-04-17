using Microsoft.EntityFrameworkCore;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Application.Mappings;
using Portfolio.API.Application.Services;
using Portfolio.API.Application.Settings;
using Portfolio.API.Infrastructure.Context;

namespace Portfolio.API.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
         this IServiceCollection services,
         IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<FeeSettings>(configuration.GetSection("FeeSettings"));

        services.AddAutoMapper(cfg => cfg.AddProfile<WalletMapping>());
        services.AddAutoMapper(cfg => cfg.AddProfile<AssetMapping>());
        services.AddAutoMapper(cfg => cfg.AddProfile<TransactionMapping>());
        services.AddAutoMapper(cfg => cfg.AddProfile<LimitOrderMapping>());

        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ILimitOrderService, LimitOrderService>();

        return services;
    }
}
