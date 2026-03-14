namespace Market.API.Application.DTOs;

public class CoinDto
{
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public double CurrentPrice { get; set; }
    public double MarketCap { get; set; }
}
