using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;
using Market.API.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Market.API.Infrastructure.Repositories;

public class PriceHistoryRepository(ApplicationDbContext context) : Repository<PriceHistory>(context), IPriceHistoryRepository
{
    public async Task<PriceHistory> GetPriceHistory(string symbol, TimeSpan howMuchPast)
    {
        var wantedDate = DateTime.UtcNow.Subtract(howMuchPast);

        return await context.PriceHistories
            .Where(x => x.Symbol == symbol)
            .OrderBy(x => x.Timestamp)
            .FirstAsync(x => x.Timestamp >= wantedDate);
    }
}
