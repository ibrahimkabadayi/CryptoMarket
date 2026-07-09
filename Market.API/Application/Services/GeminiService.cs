using DnsClient.Internal;
using Market.API.Application.Interfaces;
using Market.API.Application.Settings;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Market.API.Application.Services;

public class GeminiService(IOptions<GeminiSettings> options, ILogger<GeminiService> logger, HttpClient httpClient) : IGeminiService
{
    private const string GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1/models";

    public async Task<string> GenerateMarketNewsAsync(List<string> coinSymbols, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        Console.WriteLine("\nAPI key is: " + settings.ApiKey + "\n");

        if (string.IsNullOrEmpty(settings.ApiKey))
            throw new InvalidOperationException("Gemini API Key is not configured.");

        if (coinSymbols == null || coinSymbols.Count == 0)
            throw new ArgumentException("At least one coin symbol must be provided.");

        try
        {
            var coinsText = string.Join(", ", coinSymbols);
            var prompt = $@"Generate a realistic and informative market news article about cryptocurrencies {coinsText}. 
            The news should be:
            - Between 150-300 words
            - Professional and informative
            - Include specific details about price movements, market trends, or recent developments
            - Start with a compelling headline
            - Format: Start with ##Headline, then the content

            Generate one news article now.";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    maxOutputTokens = settings.MaxTokens,
                    temperature = 0.7
                }
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var url = $"{GeminiApiBaseUrl}/{settings.Model}:generateContent?key={settings.ApiKey}";
            var response = await httpClient.PostAsync(url, jsonContent, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Gemini API returned {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonResponse = JsonDocument.Parse(responseContent);
            var textContent = jsonResponse.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            logger.LogInformation("Successfully generated market news for coins: {Coins}", coinsText);
            return textContent ?? string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating market news for coins: {Coins}", string.Join(", ", coinSymbols));
            throw;
        }
    }
}
