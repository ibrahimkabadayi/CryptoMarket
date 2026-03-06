using Microsoft.EntityFrameworkCore;
using Portfolio.API.Domain.Interfaces;
using Portfolio.API.Infrastructure.Context;
using Portfolio.API.Infrastructure.Repositories;

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

        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<ITransacitonRepository, TransactionRepository>();

        return services;
    }
}
