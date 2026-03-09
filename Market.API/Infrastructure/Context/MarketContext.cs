using Market.API.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Market.API.Infrastructure.Context;

public class MarketDbContext
{
    public IMongoDatabase Database { get; }

    public MarketDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        Database = client.GetDatabase(settings.Value.DatabaseName);
    }
}