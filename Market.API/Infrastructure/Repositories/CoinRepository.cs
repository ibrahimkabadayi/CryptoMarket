using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;
using Market.API.Infrastructure.Context;
using MongoDB.Driver;

namespace Market.API.Infrastructure.Repositories;
public class CoinRepository(MarketDbContext context) : Repository<Coin>(context!, "Coins"), ICoinRepository
{
    public async Task<Coin> GetCoinAsync(string symbol)
    {
        return await context.Coins.Find(x => x.Symbol == symbol).FirstAsync();
    }
}