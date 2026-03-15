namespace Market.API.Domain.Entities;

public class PriceHistory : BaseEntity
{
    public string Symbol { get; set; } = string.Empty;
    public decimal OpenPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal Volume { get; set; }
    public DateTime Timestamp { get; set; }
}
