namespace Market.API.Domain.Entities;

public class PriceHistory : BaseEntity
{
    public string Symbol { get; set; } = string.Empty;
    public double OpenPrice { get; set; }
    public double ClosePrice { get; set; }
    public double HighPrice { get; set; }
    public double LowPrice { get; set; }
    public double Volume { get; set; }
    public DateTime Timestamp { get; set; }
}
