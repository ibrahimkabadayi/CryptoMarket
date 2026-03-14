namespace Market.API.Infrastructure.Context;
public class MongoDbSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string CoinsCollectionName { get; set; } = null!;
    public string MarketNewsCollectionName { get; set; } = null!;
    public string PriceHistoriesCollectionName { get; set; } = null!;
}
