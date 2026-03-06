using Microsoft.EntityFrameworkCore;
using Portfolio.API.Application.Interfaces;
using Portfolio.API.Application.Services;
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

        //services.AddAutoMapper(cfg => cfg.AddProfile<UserMapping>());

        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<ITransactionService, TransactionService>();

        return services;
    }
}
