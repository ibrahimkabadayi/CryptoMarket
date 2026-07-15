using Market.API.Domain.Entities;
using Market.API.Domain.Interfaces;
using Market.API.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Market.API.Infrastructure.Repositories;

public class PriceHistoryRepository(ApplicationDbContext context) : Repository<PriceHistory>(context), IPriceHistoryRepository
{
    public async Task<PriceHistory> GetPriceHistory(string symbol, TimeSpan howMuchPast)
    {
        var targetDate = DateTime.UtcNow.Subtract(howMuchPast);

        var priceHistory = await context.PriceHistories
            .Where(x => x.Symbol == symbol && x.Timestamp <= targetDate)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync();

        priceHistory ??= await context.PriceHistories
                .Where(x => x.Symbol == symbol && x.Timestamp > targetDate)
                .OrderBy(x => x.Timestamp)
                .FirstAsync();

        return priceHistory;
    }
}
