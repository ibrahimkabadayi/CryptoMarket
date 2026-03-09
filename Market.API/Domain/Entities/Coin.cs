using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Market.API.Domain.Entities;

public class Coin : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public double CurrentPrice { get; set; }
    public double MarketCap { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
