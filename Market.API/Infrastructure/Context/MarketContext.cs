using Market.API.Domain.Entities;
using MongoDB.Driver;

namespace Market.API.Infrastructure.Context;

public class MarketContext : IMarketContext
{
    private readonly IMongoDatabase _database;

    public MarketContext(IMongoClient mongoClient, IConfiguration configuration)
    {
        var databaseName = configuration.GetValue<string>("DatabaseSettings:DatabaseName");
        _database = mongoClient.GetDatabase(databaseName);
    }

    public IMongoCollection<Basket> Baskets => _database.GetCollection<Basket>("Baskets");
}
