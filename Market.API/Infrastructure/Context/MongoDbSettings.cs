namespace Market.API.Infrastructure.Context;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string CoinsCollectionName { get; set; } = null!;
}
