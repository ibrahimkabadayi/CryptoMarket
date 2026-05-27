using Market.API.Application.Interfaces;
using Market.API.Application.Mappings;
using Market.API.Application.Services;
using Market.API.Application.Settings;

namespace Market.API.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddAutoMapper(cfg => cfg.AddProfile<CoinMapping>());
        services.AddAutoMapper(cfg => cfg.AddProfile<MarketNewsMapping>());

        services.AddScoped<ICoinService, CoinService>();
        services.AddScoped<IMarketNewsService, MarketNewsService>();
        services.AddScoped<IPriceHistoryService, PriceHistoryService>();
        services.AddScoped<IPriceHistoryService, PriceHistoryService>();
        services.AddScoped<ILimitOrderService, LimitOrderService>();

        services.Configure<GeminiSettings>(configuration.GetSection("Gemini"));
        services.AddHttpClient<IGeminiService, GeminiService>();

        return services;
    }
}
