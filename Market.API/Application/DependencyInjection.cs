using Market.API.Application.Interfaces;
using Market.API.Application.Services;

namespace Market.API.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices() { 
        IServiceCollection services = new ServiceCollection();

        //services.AddAutoMapper(cfg => cfg.AddProfile<UserMapping>());

        services.AddScoped<ICoinService, CoinService>();
        services.AddScoped<IMarketNewsService, MarketNewsService>();
        services.AddScoped<IPriceHistoryService, PriceHistoryService>();

        throw new NotImplementedException();
    }
}
