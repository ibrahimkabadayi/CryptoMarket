namespace Market.API.Application.DTOs;

public class CoinDto
{
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal Supply { get; set; }
    public bool IsCapped { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal MarketCap { get; set; }
    public string IconUrlPng { get; set; } = string.Empty;
}
