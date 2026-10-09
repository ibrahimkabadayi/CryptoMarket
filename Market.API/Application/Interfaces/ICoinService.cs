using Market.API.Application.DTOs;

namespace Market.API.Application.Interfaces;

public interface ICoinService
{
    Task<List<CoinDto>> GetAllCoins();
    Task<CoinDto> GetCoinBySymbol(string symbol);
    void BuyCoin(BuyCoinDto buyCoinDto);
}