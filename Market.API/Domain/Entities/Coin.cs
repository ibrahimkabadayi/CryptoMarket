namespace Market.API.Domain.Entities;

public class Coin : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal Supply { get; set; }
    public bool IsCapped { get; set; }
    public decimal MarketCap { get; set; }
    public string IconUrlPng { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
