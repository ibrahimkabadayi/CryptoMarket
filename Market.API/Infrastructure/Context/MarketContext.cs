using Market.API.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Market.API.Infrastructure.Context;

public class MarketDbContext
{
    public IMongoDatabase Database { get; }
    public IMongoCollection<Coin> Coins { get; }
    public IMongoCollection<MarketNews> MarketNews { get; }
    public IMongoCollection<PriceHistory> PriceHistories { get; }

    public MarketDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        Database = client.GetDatabase(settings.Value.DatabaseName);

        Coins = Database.GetCollection<Coin>("Coins");
        MarketNews = Database.GetCollection<MarketNews>("MarketNews");
        PriceHistories = Database.GetCollection<PriceHistory>("PriceHistories");
    }
}