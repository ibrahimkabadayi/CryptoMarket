using Market.API.Application.DTOs;

namespace Market.API.Application.Interfaces;

public interface ICoinService
{
    Task<string> AddCoin(string name, string symbol, decimal price, decimal marketCap);
    Task UpdateCoin(string symbol, decimal? price, decimal? marketCap);
    Task<List<CoinDto>> GetAllCoins();
    Task<CoinDto> GetCoinBySymbol(string symbol);
    void BuyCoin(BuyCoinDto buyCoinDto);
}