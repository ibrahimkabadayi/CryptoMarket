using Market.API.Domain.Entities;

namespace Market.API.Domain.Interfaces;

public interface IPriceHistoryRepository : IRepository<PriceHistory>
{
    public Task<PriceHistory> GetPriceHistory(string symbol, TimeSpan howMuchPast);
}