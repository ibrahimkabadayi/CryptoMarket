using Market.API.Application.Interfaces;
using Market.API.Application.Mappings;
using Market.API.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Market.API.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {

        services.AddAutoMapper(cfg => cfg.AddProfile<CoinMapping>());

        services.AddScoped<ICoinService, CoinService>();
        services.AddScoped<IMarketNewsService, MarketNewsService>();
        services.AddScoped<IPriceHistoryService, PriceHistoryService>();

        return services;
    }
}
