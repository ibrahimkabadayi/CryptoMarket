namespace Market.API.Application.Interfaces;

public interface IGeminiService
{
    Task<string> GenerateMarketNewsAsync(List<string> coinSymbols, CancellationToken cancellationToken = default);
}
