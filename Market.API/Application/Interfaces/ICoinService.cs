using Market.API.Application.DTOs;

namespace Market.API.Application.Interfaces;

public interface ICoinService
{
    Task<string> AddCoin(string Name, string Symbol, decimal Price, decimal MarketCap);
    Task<List<CoinDto>> GetAllCoins();
    Task<CoinDto> GetCoinBySymbol(string Symbol);
    void BuyCoin(BuyCoinDto buyCoinDto);
}