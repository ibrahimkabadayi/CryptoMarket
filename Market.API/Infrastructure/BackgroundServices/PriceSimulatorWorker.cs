namespace Market.API.Infrastructure.BackgroundServices;

public class PriceSimulatorWorker(
    IHttpClientFactory httpClientFactory,
    ILogger<PriceSimulatorWorker> logger) : BackgroundService
{

    // Mevcut gerçek fiyatları tutan dictionary
    private readonly Dictionary<string, decimal> _realPrices = new()
    {
        ["bitcoin"] = 83000m,
        ["ethereum"] = 1800m,
        ["solana"] = 130m
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // İki görevi paralel çalıştır
        await Task.WhenAll(
            FetchRealPricesLoop(stoppingToken),
            SimulatePricesLoop(stoppingToken)
        );
    }

    // Görev 1: Her 2 saniyede gerçek fiyatı çek
    private async Task FetchRealPricesLoop(CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("coingecko");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var ids = string.Join(",", _realPrices.Keys);
                var response = await client
                    .GetFromJsonAsync<Dictionary<string, Dictionary<string, decimal>>>(
                        $"simple/price?ids={ids}&vs_currencies=usd", ct);

                if (response is not null)
                {
                    foreach (var (coin, prices) in response)
                        _realPrices[coin] = prices["usd"];

                    logger.LogInformation("Gerçek fiyatlar güncellendi: BTC={Btc}", _realPrices["bitcoin"]);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CoinGecko fiyat çekme hatası");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
    }

    // Görev 2: Her 100ms'de fiyatı dalgalandır ve yayınla
    private async Task SimulatePricesLoop(CancellationToken ct)
    {
        var rng = new Random();

        while (!ct.IsCancellationRequested)
        {
            foreach (var (coin, basePrice) in _realPrices)
            {
                var fluctuation = (decimal)(rng.NextDouble() * 0.001 - 0.0005);
                var simulatedPrice = basePrice * (1 + fluctuation);
            }

            await Task.Delay(100, ct); // 100ms = 0.1 saniye
        }
    }
}
