namespace Market.API.Models;

public class AddCoinRequest
{
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public double Price { get; set; } 
    public double MarketCap { get; set; }

}
