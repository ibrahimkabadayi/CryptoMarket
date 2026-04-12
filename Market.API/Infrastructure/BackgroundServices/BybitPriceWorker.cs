using System.Text.Json.Serialization;
using Bybit.Net.Interfaces.Clients;
using MassTransit;
using Shared.Messages;

namespace Market.API.Infrastructure.BackgroundServices;

public class BybitPriceWorker : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BybitPriceWorker> _logger;

    private readonly Dictionary<string, decimal> _realPrices = new()
    {
        ["BTCUSDT"] = 70000m,
        ["ETHUSDT"] = 1800m,
        ["SOLUSDT"] = 130m
    };

    public BybitPriceWorker(IHttpClientFactory httpClientFactory, ILogger<BybitPriceWorker> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.WhenAll(
            FetchRealPricesLoop(stoppingToken),
            SimulatePricesLoop(stoppingToken)
        );
    }

    private async Task FetchRealPricesLoop(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("bybit");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                foreach (var symbol in _realPrices.Keys.ToList())
                {
                    var response = await client.GetFromJsonAsync<BybitResponse>(
                        $"v5/market/tickers?category=spot&symbol={symbol}", ct);

                    if (response?.RetCode == 0)
                    {
                        var price = decimal.Parse(
                            response.Result.List[0].LastPrice,
                            System.Globalization.CultureInfo.InvariantCulture);

                        _realPrices[symbol] = price;
                        _logger.LogInformation("{Symbol}: {Price}", symbol, price);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bybit fiyat çekme hatası");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }

    private async Task SimulatePricesLoop(CancellationToken ct)
    {
        var rng = new Random();

        while (!ct.IsCancellationRequested)
        {
            foreach (var (symbol, basePrice) in _realPrices)
            {
                var fluctuation = (decimal)(rng.NextDouble() * 0.001 - 0.0005);
                var simulatedPrice = Math.Round(basePrice * (1 + fluctuation), 2);

                _logger.LogInformation("[Sim] {Symbol}: {Price}", symbol, simulatedPrice);
                // buraya hub push logiğini ekle
            }

            await Task.Delay(100, ct);
        }
    }
}

// Response modelleri
public record BybitResponse(
    [property: JsonPropertyName("retCode")] int RetCode,
    [property: JsonPropertyName("result")] BybitResult Result
);

public record BybitResult(
    [property: JsonPropertyName("list")] List<BybitTicker> List
);

public record BybitTicker(
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("lastPrice")] string LastPrice
);
