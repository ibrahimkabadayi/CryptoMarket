using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;
using Market.API.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace Market.API.Infrastructure.Repositories;

public class CoinRepository(ApplicationDbContext context) : Repository<Coin>(context), ICoinRepository
{
    public async Task<Coin> GetCoinAsync(string symbol)
    {
        return await context.Coins.FirstAsync(x =>  x.Symbol == symbol);
    }

    public async Task UpdateCoinSupply (string symbol, decimal supply)
    {
        await context.Coins
            .Where(x => x.Symbol == symbol)
            .ExecuteUpdateAsync(x => x
            .SetProperty(y => y.Supply, supply)
            .SetProperty(y => y.LastUpdated, DateTime.UtcNow));
    }
    
    public async Task UpdateCoinPriceAsync(string symbol, decimal price, decimal marketCap)
    {
        await context.Coins
            .Where(x => x.Symbol == symbol)
            .ExecuteUpdateAsync(x => x
                .SetProperty(y => y.CurrentPrice, price)
                .SetProperty(y => y.MarketCap, marketCap)
                .SetProperty(y => y.LastUpdated, DateTime.UtcNow));
    }
}