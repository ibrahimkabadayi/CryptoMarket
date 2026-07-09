using Market.API.Domain.Entities;

namespace Market.API.Domain.Interfaces;

public interface ICoinRepository : IRepository<Coin>
{
    Task<Coin> GetCoinAsync(string symbol);
    Task UpdateCoinSupply(string Symbol, decimal Supply);
    Task UpdateCoinSupplyAndCap(string symbol, decimal supply, bool isCapped);
}
