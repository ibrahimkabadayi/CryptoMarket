using Market.API.Application.Interfaces;
using Market.API.Domain.Interfaces;

namespace Market.API.Infrastructure.BackgroundServices;

public class MarketNewsGenerator(
    IServiceScopeFactory scopeFactory,
    ILogger<MarketNewsGenerator> logger) : BackgroundService
{
    private const int IntervalMinutes = 30;
    private readonly Random _random = new();
    private int _executionCount = 0;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Market News Generator Background Service started. Will run every {Minutes} minutes.", IntervalMinutes);

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _executionCount++;
                logger.LogInformation("[{ExecutionCount}] Starting market news generation cycle.", _executionCount);

                using var scope = scopeFactory.CreateScope();
                var coinService = scope.ServiceProvider.GetRequiredService<ICoinService>();
                var geminiService = scope.ServiceProvider.GetRequiredService<IGeminiService>();
                var marketNewsService = scope.ServiceProvider.GetRequiredService<IMarketNewsService>();

                var allCoins = await coinService.GetAllCoins();
                if (allCoins == null || allCoins.Count == 0)
                {
                    logger.LogWarning("No coins found in repository. Skipping news generation.");
                    await DelayUntilNextExecution(stoppingToken);
                    continue;
                }

                logger.LogInformation("Found {CoinCount} coins in database: {Coins}",
                    allCoins.Count, string.Join(", ", allCoins.Select(c => c.Symbol)));

                var numberOfCoins = _random.Next(1, Math.Min(4, allCoins.Count + 1));
                var selectedCoins = allCoins
                    .OrderBy(_ => _random.Next())
                    .Take(numberOfCoins)
                    .Select(c => c.Symbol)
                    .ToList();

                logger.LogInformation("[{ExecutionCount}] Selected {Count} coins for news generation: {Coins}",
                    _executionCount, selectedCoins.Count, string.Join(", ", selectedCoins));

                try
                {
                    var newsContent = await geminiService.GenerateMarketNewsAsync(selectedCoins, stoppingToken);

                    if (string.IsNullOrEmpty(newsContent))
                    {
                        logger.LogWarning("[{ExecutionCount}] Gemini returned empty content for coins: {Coins}",
                            _executionCount, string.Join(", ", selectedCoins));
                        await DelayUntilNextExecution(stoppingToken);
                        continue;
                    }

                    logger.LogDebug("[{ExecutionCount}] Raw Gemini content length: {Length}", _executionCount, newsContent.Length);

                    var (title, content) = ParseNewsContent(newsContent);

                    logger.LogInformation("[{ExecutionCount}] Parsed title: {Title}", _executionCount, title);
                    logger.LogDebug("[{ExecutionCount}] Parsed content length: {ContentLength}", _executionCount, content.Length);

                    await marketNewsService.CreateMarketNewsAsync(title, content, selectedCoins);

                    logger.LogInformation("[{ExecutionCount}] ✅ Market news generated successfully for coins: {Coins}",
                        _executionCount, string.Join(", ", selectedCoins));
                }
                catch (Exception geminiEx)
                {
                    logger.LogError(geminiEx, "[{ExecutionCount}] Error calling Gemini service for coins: {Coins}",
                        _executionCount, string.Join(", ", selectedCoins));
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Market News Generator Background Service cancellation requested.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[{ExecutionCount}] Unexpected error in market news generation", _executionCount);
            }

            await DelayUntilNextExecution(stoppingToken);
        }

        logger.LogInformation("Market News Generator Background Service stopped.");
    }

    private async Task DelayUntilNextExecution(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Next execution in {Minutes} minutes.", IntervalMinutes);
            await Task.Delay(TimeSpan.FromMinutes(IntervalMinutes), stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private (string title, string content) ParseNewsContent(string newsContent)
    {
        if (string.IsNullOrEmpty(newsContent))
            return ("Market Update", "Unable to generate news at this time.");

        var lines = newsContent.Split(["\n", "\r\n"], StringSplitOptions.None);

        var titleLine = lines.FirstOrDefault(l => l.StartsWith("##"))?.Replace("##", "").Trim();
        var title = !string.IsNullOrEmpty(titleLine) ? titleLine : "Cryptocurrency Market Update";

        var contentStartIndex = string.IsNullOrEmpty(titleLine) ? 0 : Array.IndexOf(lines, lines.FirstOrDefault(l => l.StartsWith("##"))) + 1;
        var contentLines = lines.Skip(contentStartIndex)
            .Where(l => !string.IsNullOrEmpty(l.Trim()))
            .ToList();

        var content = string.Join("\n", contentLines).Trim();

        if (content.Length < 50)
        {
            content = newsContent;
        }

        return (title, content);
    }
}