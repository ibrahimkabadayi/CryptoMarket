namespace Market.API.Infrastructure.Context;

using Market.API.Domain.Entities;
using MongoDB.Driver;

public interface IMarketContext
{
    public IMongoCollection<Basket> Baskets { get; }
}
